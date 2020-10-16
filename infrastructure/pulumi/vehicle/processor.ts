import * as path from 'path'
import * as config from '../config'
import { eventstore } from '../eventstore'
import { seq } from '../seq'
import { registry } from '../registry'
import { dealershipNamespace, registrySecret, eventstoreSecret } from '../k8s'
import { Service } from '../service'

export const vehicleProcessor = new Service('vehicle-processor', {
    namespace: dealershipNamespace,
    registry: registry,
    context: path.join(config.root, 'vehicle'),
    dockerfile: path.join(config.root, 'vehicle', 'deploy', 'processor.Dockerfile'),
    target: 'runner',
    backendType: 'grpc',
    imagePullSecret: registrySecret,
    secrets: [ eventstoreSecret ],
    env: {
        EVENTSTORE_SECRET: eventstoreSecret.metadata.name,
        EVENTSTORE_SCHEME: 'discover',
        EVENTSTORE_HOST: eventstore.internalHost,
        EVENTSTORE_PORT: eventstore.internalPort.apply(port => `${port}`),
        EVENTSTORE_USER: 'admin',
        SEQ_SCHEME: 'http',
        SEQ_HOST: seq.internalHost,
        SEQ_PORT: seq.internalIngestionPort.apply(port => `${port}`)
    }
}, { provider: config.k8sProvider })
