import * as path from 'path'
import * as config from './config'
import { Client } from './client'
import { registry } from './registry'
import { dealershipNamespace, registrySecret, auth0Secret } from './k8s'
import { externalGateway } from './gateway'
import { zone } from './cloudflare'
import { Service } from './service'
import { seq } from './seq'
import { vehicleProcessor, vehicleReader } from './vehicle'

export const graphqlApi = new Service('graphql-api', {
    namespace: dealershipNamespace,
    registry: registry,
    context: path.join(config.root, 'client', 'graphql-api'),
    dockerfile: path.join(config.root, 'client', 'graphql-api', 'Dockerfile'),
    target: 'runner',
    backendType: 'http',
    secrets: [ auth0Secret ],
    imagePullSecret: registrySecret,
    env: {
        AUTH0_SECRET: auth0Secret.metadata.name,
        VEHICLE_PROCESSOR_HOST: vehicleProcessor.internalHost!,
        VEHICLE_PROCESSOR_PORT: vehicleProcessor.internalPort!.apply(port => `${port}`),
        VEHICLE_READER_HOST: vehicleReader.internalHost!,
        VEHICLE_READER_PORT: vehicleReader.internalPort!.apply(port => `${port}`),
        SEQ_SCHEME: 'http',
        SEQ_HOST: seq.internalHost,
        SEQ_PORT: seq.internalIngestionPort.apply(port => `${port}`)
    }
}, { provider: config.k8sProvider })

export const graphqlApiClient = new Client('graphql-api', {
    namespace: dealershipNamespace,
    gateway: externalGateway,
    zone: zone,
    subdomain: 'graphql',
    authUrl: config.auth0Config.domain,
    serviceHost: graphqlApi.internalHost,
    servicePort: graphqlApi.internalPort
}, { providers: [ config.cloudflareProvider, config.k8sProvider, config.auth0Provider ] })
