import * as path from 'path'
import * as config from '../config'
import { mongo } from '../mongo'
import { seq } from '../seq'
import { registry } from '../registry'
import { dealershipNamespace, registrySecret, mongoSecret } from '../k8s'
import { Service } from '../service'

export const vehicleReader = new Service('vehicle-reader', {
    namespace: dealershipNamespace,
    registry: registry,
    context: path.join(config.root, 'vehicle'),
    dockerfile: path.join(config.root, 'vehicle', 'deploy', 'reader.Dockerfile'),
    target: 'runner',
    backendType: 'grpc',
    imagePullSecret: registrySecret,
    secrets: [ mongoSecret ],
    env: {
        MONGO_SECRET: mongoSecret.metadata.name,
        MONGO_HOST: mongo.internalHost,
        MONGO_PORT: mongo.internalPort.apply(port => `${port}`),
        MONGO_DATABASE: config.mongoConfig.database,
        SEQ_SCHEME: 'http',
        SEQ_HOST: seq.internalHost,
        SEQ_PORT: seq.internalIngestionPort.apply(port => `${port}`)
    }
}, { provider: config.k8sProvider })
