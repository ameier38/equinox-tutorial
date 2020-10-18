import * as auth0 from '@pulumi/auth0'
import * as cloudflare from '@pulumi/cloudflare'
import * as pulumi from '@pulumi/pulumi'
import * as k8s from '@pulumi/kubernetes'
import * as path from 'path'
import * as config from './config'
import { Service } from './service'
import { Gateway, externalGateway } from './gateway'
import { GraphqlApi } from './graphqlApi'
import { Registry, registry } from './registry'
import { iconUrl } from './bucket'
import { dealershipNamespace } from './k8s'
import { zone } from './cloudflare'

type WebAppArgs = {
    zone: cloudflare.Zone
    audience: pulumi.Input<string> 
    authUrl: pulumi.Input<string>
    namespace: k8s.core.v1.Namespace
    registry: Registry
    iconUrl: pulumi.Input<string>
    gateway: Gateway
    graphqlApi: GraphqlApi
}

class WebApp extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>
    host: pulumi.Output<string>

    constructor(name:string, args:WebAppArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:WebApp', name, {}, opts)

        const record = new cloudflare.Record(`${name}-web-app`, {
            zoneId: args.zone.id,
            // NB: root
            name: '@',
            type: 'CNAME',
            value: args.gateway.loadBalancerHost
        }, { parent: this })

        this.host = record.hostname

        const registrySecret = new k8s.core.v1.Secret(`${name}-web-app-registry`, {
            metadata: { namespace: args.namespace.metadata.name },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.registry.dockerCredentials
            }
        }, { parent: this })

        const client = new auth0.Client(`${name}-web-app`, {
            name: `${name}-web-app`,
            logoUri: args.iconUrl,
            appType: 'spa',
            callbacks: [
                'http://localhost:3000',
                pulumi.interpolate `https://${this.host}`,
            ],
            allowedLogoutUrls: [
                'http://localhost:3000',
                pulumi.interpolate `https://${this.host}`,
            ],
        }, { provider: config.auth0Provider })

        const service = new Service(`${name}-web-app`, {
            namespace: args.namespace,
            registry: registry,
            context: path.join(config.root, 'client', 'web-app'),
            dockerfile: path.join(config.root, 'client', 'web-app', 'deploy', 'customer.Dockerfile'),
            buildTarget: 'runner',
            buildArgs: {
                RUNTIME_IMAGE: 'nginx:1.17-alpine',
                APP_SCHEME: 'https',
                APP_HOST: this.host,
                APP_PORT: '80',
                AUTH0_DOMAIN: args.authUrl,
                AUTH0_CLIENT_ID: client.clientId,
                AUTH0_AUDIENCE: args.audience, 
                GRAPHQL_API_SCHEME: 'https',
                GRAPHQL_API_HOST: args.graphqlApi.host,
                GRAPHQL_API_PORT: '80'
            },
            imagePullSecret: registrySecret,
            backendType: 'http',
            secrets: [],
            env: {}
        }, { parent: this })

        this.internalHost = service.internalHost!
        this.internalPort = service.internalPort!

        // NB: specifies how to direct incoming requests
        new k8s.apiextensions.CustomResource(name, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Mapping',
            metadata: { namespace: args.namespace.metadata.name },
            spec: {
                prefix: '/',
                host: this.host,
                service: pulumi.interpolate `${this.internalPort}:${this.internalPort}`
            }
        }, { parent: this, dependsOn: args.gateway })

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort,
            host: this.host
        })
    }
}

// export const webApp = new WebApp('v1', {
//     namespace: dealershipNamespace,
//     registry: registry,
//     gateway: externalGateway,
//     audience: config.audience,
//     authUrl: config.authUrl,
//     zone: zone,
//     iconUrl: iconUrl,
//     graphqlApi: graphqlApi
// }, { providers: [ config.k8sProvider, config.auth0Provider, config.cloudflareProvider ]})
