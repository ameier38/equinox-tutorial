import * as cloudflare from '@pulumi/cloudflare'
import * as docker from '@pulumi/docker'
import * as pulumi from '@pulumi/pulumi'
import * as k8s from '@pulumi/kubernetes'
import * as path from 'path'
import * as config from './config'
import { identityProvider } from './identityProvider'
// import { graphqlApi } from './graphqlApi'
import { cosmicdealershipNamespace } from './namespace'
import { zone } from './zone'

type WebAppArgs = {
    zoneId: pulumi.Input<string>
    authDomain: pulumi.Input<string>
    clientId: pulumi.Input<string>
    audience: pulumi.Input<string> 
    namespace: pulumi.Input<string>
    registryEndpoint: pulumi.Input<string>
    imageRegistry: pulumi.Input<docker.ImageRegistry>
    dockerCredentials: pulumi.Input<string>
    loadBalancerAddress: pulumi.Input<string>
    graphqlApiHost: pulumi.Input<string>
}

class WebApp extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>
    host: pulumi.Output<string>

    constructor(name:string, args:WebAppArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:WebApp', name, {}, opts)

        const record = new cloudflare.Record(`${name}-web-app`, {
            zoneId: args.zoneId,
            // NB: root
            name: '@',
            type: 'CNAME',
            value: args.loadBalancerAddress
        }, { parent: this })

        this.host = record.hostname

        const registrySecret = new k8s.core.v1.Secret(`${name}-web-app-registry`, {
            metadata: { namespace: args.namespace },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.dockerCredentials
            }
        }, { parent: this })

        const image = new docker.Image(name, {
            imageName: pulumi.interpolate `${args.registryEndpoint}/cosmicdealership/${name}-web-app`,
            build: {
                context: path.join(config.root, 'web-app'),
                dockerfile: path.join(config.root, 'web-app', 'docker', 'customer.Dockerfile'),
                target: 'runner',
                args: {
                    APP_SCHEME: 'https',
                    APP_HOST: this.host,
                    APP_PORT: '80',
                    AUTH_DOMAIN: args.authDomain,
                    AUTH_AUDIENCE: args.audience, 
                    AUTH_CLIENT_ID: args.clientId,
                    GRAPHQL_API_SCHEME: 'https',
                    GRAPHQL_API_HOST: args.graphqlApiHost,
                    GRAPHQL_API_PORT: '80'
                }
            },
            registry: args.imageRegistry
        }, { parent: this })

        const chartName = `${name}-web-app`
        const chart = new k8s.helm.v3.Chart(name, {
            chart: 'base-service',
            version: '0.1.2',
            fetchOpts: {
                repo: 'https://ameier38.github.io/charts'
            },
            namespace: args.namespace,
            values: {
                fullnameOverride: chartName,
                image: image.imageName,
                imagePullSecrets: [registrySecret.metadata.name],
                backendType: 'http'
            }
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, chartName, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalPort =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, chartName, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'http')!.port)

        // NB: specifies how to direct incoming requests
        new k8s.apiextensions.CustomResource(name, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Mapping',
            metadata: { namespace: args.namespace },
            spec: {
                prefix: '/',
                host: this.host,
                service: pulumi.interpolate `${this.internalPort}:${this.internalPort}`
            }
        }, { parent: this })

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort,
            host: this.host
        })
    }
}

// export const webApp = new WebApp('v1', {
//     namespace: cosmicdealershipNamespace.metadata.name,
//     registryEndpoint: config.registryEndpoint,
//     imageRegistry: config.imageRegistry,
//     dockerCredentials: config.dockerCredentials,
//     authDomain: config.auth0Config.domain,
//     clientId: identityProvider.webAppClientId,
//     audience: config.audience,
//     zoneId: zone.id,
//     graphqlApiHost: graphqlApi.host,
//     loadBalancerAddress: config.loadBalancerAddress
// }, { providers: [ config.k8sProvider, config.cloudflareProvider ]})
