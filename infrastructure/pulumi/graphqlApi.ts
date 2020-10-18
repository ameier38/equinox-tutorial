import * as auth0 from '@pulumi/auth0'
import * as cloudflare from '@pulumi/cloudflare'
import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'
import * as config from './config'
import { Registry, registry } from './registry'
import { dealershipNamespace } from './k8s'
import { Gateway, externalGateway } from './gateway'
import { zone } from './cloudflare'
import { Service } from './service'
import { Seq } from './seq'
import { VehicleProcessor, VehicleReader } from './vehicle'

type GraphqlApiArgs = {
    namespace: k8s.core.v1.Namespace
    registry: Registry
    audience: pulumi.Input<string>
    zone: cloudflare.Zone
    subdomain: pulumi.Input<string>
    gateway: Gateway
    authUrl: pulumi.Input<string>
    vehicleProcessor: VehicleProcessor
    vehicleReader: VehicleReader
    seq: Seq
}

export class GraphqlApi extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>
    host: pulumi.Output<string>

    constructor(name:string, args:GraphqlApiArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:GraphqlApi', name, {}, opts)

        const record = new cloudflare.Record(`${name}-graphql-api`, {
            zoneId: args.zone.id,
            name: args.subdomain,
            type: 'CNAME',
            value: args.gateway.loadBalancerHost
        }, { parent: this })

        this.host = record.hostname

        // NB: used by ambassador to validate the token
        // ref: https://auth0.com/docs/applications
        const client = new auth0.Client(`${name}-graphql-api`, {
            name: name,
            description: `${name} client`,
            appType: 'non_interactive',
            tokenEndpointAuthMethod: 'client_secret_post',
            callbacks: [
                pulumi.interpolate `https://${this.host}/.ambassador/oauth2/redirection-endpoint`,
            ],
        }, { parent: this })

        const registrySecret = new k8s.core.v1.Secret(`${name}-graphql-api-registry`, {
            metadata: { namespace: args.namespace.metadata.name },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.registry.dockerCredentials
            }
        }, { parent: this })

        const auth0Secret = new k8s.core.v1.Secret(`${name}-graphql-api-auth0`, {
            metadata: { namespace: args.namespace.metadata.name },
            stringData: {
                'client-secret': client.clientSecret
            }
        }, { parent: this })

        const service = new Service(`${name}-graphql-api`, {
            namespace: args.namespace,
            registry: registry,
            context: path.join(config.root, 'client', 'graphql-api'),
            dockerfile: path.join(config.root, 'client', 'graphql-api', 'Dockerfile'),
            buildTarget: 'runner',
            buildArgs: {},
            backendType: 'http',
            secrets: [auth0Secret],
            imagePullSecret: registrySecret,
            env: {
                AUTH0_SECRET: auth0Secret.metadata.name,
                VEHICLE_PROCESSOR_HOST: args.vehicleProcessor.internalHost!,
                VEHICLE_PROCESSOR_PORT: args.vehicleProcessor.internalPort!.apply(port => `${port}`),
                VEHICLE_READER_HOST: args.vehicleReader.internalHost!,
                VEHICLE_READER_PORT: args.vehicleReader.internalPort!.apply(port => `${port}`),
                SEQ_SCHEME: 'http',
                SEQ_HOST: args.seq.internalHost,
                SEQ_PORT: args.seq.internalIngestionPort.apply(port => `${port}`)
            }
        }, { parent: this })

        this.internalHost = service.internalHost!
        this.internalPort = service.internalPort!

        // NB: specifies oauth client to use for incoming requests
        // ref: https://www.getambassador.io/docs/latest/topics/using/filters/oauth2/
        const authFilter = new k8s.apiextensions.CustomResource(name, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Filter',
            metadata: { namespace: args.namespace.metadata.name },
            spec: {
                OAuth2: {
                    // NB: url which serves /.well-known/jwks.json
                    authorizationURL: args.authUrl,
                    extraAuthorizationParameters: {
                        // NB: specifying and audience will tell Auth0 to return an access token instead of opaque token
                        // ref: https://auth0.com/docs/tokens/access-tokens/get-access-tokens
                        audience: args.audience
                    },
                    clientID: client.clientId,
                    secret: client.clientSecret,
                    protectedOrigins: [{
                        origin: this.host
                    }]
                }
            }
        }, { parent: this, dependsOn: args.gateway })

        // NB: specifies which requests to apply above filter
        new k8s.apiextensions.CustomResource(name, {
            apiVersion: 'getambassador.io/v2',
            kind: 'FilterPolicy',
            metadata: { namespace: args.namespace.metadata.name },
            spec: {
                rules: [{
                    host: this.host,
                    path: '*',
                    filters: [{
                        name: authFilter.metadata.name,
                        arguments: {
                            scopes: ['openid']
                        }
                    }]
                }]
            }
        }, { parent: this, dependsOn: args.gateway })

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

// export const graphqlApi = new GraphqlApi('v1', {
//     namespace: dealershipNamespace,
//     registry: registry,
//     gateway: externalGateway,
//     audience: config.audience,
//     zone: zone,
//     subdomain: 'graphql',
//     authUrl: config.authUrl,
//     vehicleProcessor: vehicleProcessor,
//     vehicleReader: vehicleReader,
//     seq: seq
// }, { providers: [ config.k8sProvider, config.cloudflareProvider, config.auth0Provider ]})
