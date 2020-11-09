import * as cloudflare from '@pulumi/cloudflare'
import * as docker from '@pulumi/docker'
import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'
import * as config from './config'
import { identityProvider } from './identityProvider'
import { cosmicdealershipNamespace } from './namespace'
import { vehicleProcessor } from './vehicleProcessor'
import { vehicleReader } from './vehicleReader'
import { zone } from './zone'

type GraphqlApiArgs = {
    namespace: pulumi.Input<string>
    registryEndpoint: pulumi.Input<string>
    imageRegistry: pulumi.Input<docker.ImageRegistry>
    dockerCredentials: pulumi.Input<string>
    clientSecret: pulumi.Input<string>
    zoneId: pulumi.Input<string>
    subdomain: pulumi.Input<string>
    loadBalancerAddress: pulumi.Input<string>
    acmeEmail: pulumi.Input<string>
    seqHost: pulumi.Input<string>
    seqPort: pulumi.Input<string>
    vehicleProcessorHost: pulumi.Input<string>
    vehicleProcessorPort: pulumi.Input<string>
    vehicleReaderHost: pulumi.Input<string>
    vehicleReaderPort: pulumi.Input<string>
}

export class GraphqlApi extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>
    host: pulumi.Output<string>

    constructor(name:string, args:GraphqlApiArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:GraphqlApi', name, {}, opts)

        const record = new cloudflare.Record(`${name}-graphql-api`, {
            zoneId: args.zoneId,
            name: args.subdomain,
            type: 'A',
            value: args.loadBalancerAddress
        }, { parent: this })

        this.host = record.hostname

        const registrySecret = new k8s.core.v1.Secret(`${name}-graphql-api-registry`, {
            metadata: { namespace: args.namespace },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.dockerCredentials
            }
        }, { parent: this })

        // NB: validate that tokens were generated from the web client
        const authSecret = new k8s.core.v1.Secret(`${name}-graphql-api-auth`, {
            metadata: { namespace: args.namespace },
            stringData: {
                'client-secret': args.clientSecret
            }
        }, { parent: this })

        const image = new docker.Image(name, {
            imageName: pulumi.interpolate `${args.registryEndpoint}/cosmicdealership/${name}-graphql-api`,
            build: {
                context: path.join(config.root, 'graphql-api'),
                target: 'runner',
                env: { DOCKER_BUILDKIT: '1' }
            },
            registry: args.imageRegistry
        }, { parent: this })

        const chartName = `${name}-graphql-api`
        const chart = new k8s.helm.v3.Chart(name, {
            chart: 'base-service',
            version: '0.1.2',
            fetchOpts: {
                repo: 'https://ameier38.github.io/charts'
            },
            namespace: args.namespace,
            values: {
                nameOverride: chartName,
                fullnameOverride: chartName,
                image: image.imageName,
                imagePullSecrets: [registrySecret.metadata.name],
                backendType: 'http',
                containerPort: 4000,
                env: {
                    AUTH_SECRET: authSecret.metadata.name,
                    VEHICLE_PROCESSOR_HOST: args.vehicleProcessorHost,
                    VEHICLE_PROCESSOR_PORT: args.vehicleProcessorPort,
                    VEHICLE_READER_HOST: args.vehicleReaderHost,
                    VEHICLE_READER_PORT: args.vehicleReaderPort,
                    SEQ_SCHEME: 'http',
                    SEQ_HOST: args.seqHost,
                    SEQ_PORT: args.seqPort
                },
                secrets: [
                    authSecret.metadata.name
                ]
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

        // NB: generates certificate
        new k8s.apiextensions.CustomResource(`${name}-graphql-api`, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Host',
            metadata: { namespace: args.namespace },
            spec: {
                hostname: this.host,
                acmeProvider: {
                    email: args.acmeEmail
                }
            }
        }, { parent: this })

        // NB: specifies how to direct incoming requests
        new k8s.apiextensions.CustomResource(`${name}-graphql-api`, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Mapping',
            metadata: { namespace: args.namespace },
            spec: {
                prefix: '/',
                host: this.host,
                service: pulumi.interpolate `${this.internalHost}:${this.internalPort}`
            }
        }, { parent: this })

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort,
            host: this.host
        })
    }
}

// export const graphqlApi = new GraphqlApi('v1', {
//     namespace: cosmicdealershipNamespace.metadata.name,
//     registryEndpoint: config.registryEndpoint,
//     imageRegistry: config.imageRegistry,
//     dockerCredentials: config.dockerCredentials,
//     clientSecret: identityProvider.webAppClientSecret,
//     zoneId: zone.id,
//     subdomain: 'graphql',
//     loadBalancerAddress: config.loadBalancerAddress,
//     acmeEmail: config.acmeEmail,
//     seqHost: config.seqInternalHost,
//     seqPort: config.seqInternalPort.apply(p => `${p}`),
//     vehicleProcessorHost: vehicleProcessor.internalHost,
//     vehicleProcessorPort: vehicleProcessor.internalPort.apply(p => `${p}`),
//     vehicleReaderHost: vehicleReader.internalHost,
//     vehicleReaderPort: vehicleReader.internalPort.apply(p => `${p}`)
// }, { providers: [ config.k8sProvider, config.cloudflareProvider, config.auth0Provider ]})
