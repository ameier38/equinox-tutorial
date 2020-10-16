import * as path from 'path'
import * as config from '../config'
import { eventstore } from '../eventstore'
import { mongo } from '../mongo'
import { seq } from '../seq'
import { registry } from '../registry'
import { dealershipNamespace, registrySecret, eventstoreSecret, mongoSecret } from '../k8s'
import { Service } from '../service'

export const vehicleReactor = new Service('vehicle-reactor', {
    namespace: dealershipNamespace,
    registry: registry,
    context: path.join(config.root, 'vehicle'),
    dockerfile: path.join(config.root, 'vehicle', 'deploy', 'reactor.Dockerfile'),
    target: 'runner',
    backendType: 'console',
    imagePullSecret: registrySecret,
    secrets: [ eventstoreSecret, mongoSecret ],
    env: {
        EVENTSTORE_SCHEME: 'discover',
        EVENTSTORE_SECRET: eventstoreSecret.metadata.name,
        EVENTSTORE_HOST: eventstore.internalHost,
        EVENTSTORE_PORT: eventstore.internalPort.apply(port => `${port}`),
        MONGO_SECRET: mongoSecret.metadata.name,
        MONGO_HOST: mongo.internalHost,
        MONGO_PORT: mongo.internalPort.apply(port => `${port}`),
        MONGO_DATABASE: config.mongoConfig.database,
        SEQ_SCHEME: 'http',
        SEQ_HOST: seq.internalHost,
        SEQ_PORT: seq.internalIngestionPort.apply(port => `${port}`)
    }
}, { provider: config.k8sProvider })
