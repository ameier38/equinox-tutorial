import * as pulumi from '@pulumi/pulumi'
import * as k8s from '@pulumi/kubernetes'
import * as path from 'path'
import * as config from '../config'
import { EventStore, eventstore } from '../eventstore'
import { Mongo } from '../mongo'
import { Seq } from '../seq'
import { Registry, registry } from '../registry'
import { dealershipNamespace } from '../k8s'
import { Service } from '../service'

type VehicleReactorArgs = {
    namespace: k8s.core.v1.Namespace
    registry: Registry
    eventstore: EventStore
    eventstoreUser: config.EventStoreUser
    mongo: Mongo
    mongoDatabase: pulumi.Input<string>
    mongoUser: config.MongoUser
    seq: Seq
}

export class VehicleReactor extends pulumi.ComponentResource {
    constructor(name:string, args:VehicleReactorArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:VehicleReactor', name, {}, opts)

        const registrySecret = new k8s.core.v1.Secret(`${name}-vehicle-reactor-registry`, {
            metadata: { namespace: args.namespace.metadata.name },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.registry.dockerCredentials
            }
        }, { parent: this })

        const eventstoreSecret = new k8s.core.v1.Secret(`${name}-vehicle-reactor-eventstore`, {
            metadata: { namespace: args.namespace.metadata.name },
            stringData: {
                user: args.eventstoreUser.name,
                password: args.eventstoreUser.password
            }
        }, { parent: this })

        const mongoSecret = new k8s.core.v1.Secret(`${name}-vehicle-reactor-mongo`, {
            metadata: { namespace: args.namespace.metadata.name },
            stringData: {
                user: args.mongoUser.name,
                password: args.mongoUser.password
            }
        }, { parent: this })

        new Service('vehicle-reactor', {
            namespace: dealershipNamespace,
            registry: registry,
            context: path.join(config.root, 'vehicle'),
            dockerfile: path.join(config.root, 'vehicle', 'deploy', 'reactor.Dockerfile'),
            buildTarget: 'runner',
            buildArgs: {},
            backendType: 'console',
            imagePullSecret: registrySecret,
            secrets: [ eventstoreSecret, mongoSecret ],
            env: {
                EVENTSTORE_SCHEME: 'discover',
                EVENTSTORE_SECRET: eventstoreSecret.metadata.name,
                EVENTSTORE_HOST: args.eventstore.internalHost,
                EVENTSTORE_PORT: args.eventstore.internalPort.apply(port => `${port}`),
                MONGO_SECRET: mongoSecret.metadata.name,
                MONGO_HOST: args.mongo.internalHost,
                MONGO_PORT: args.mongo.internalPort.apply(port => `${port}`),
                MONGO_DATABASE: args.mongoDatabase,
                SEQ_SCHEME: 'http',
                SEQ_HOST: args.seq.internalHost,
                SEQ_PORT: args.seq.internalIngestionPort.apply(port => `${port}`)
            }
        }, { parent: this })
    }
}

// export const vehicleReactor = new VehicleReactor('v1', {
//     namespace: dealershipNamespace,
//     registry: registry,
//     eventstore: eventstore,
//     eventstoreUser: config.vehicleReactorEventStoreUser,
//     mongo: mongo,
//     mongoDatabase: config.mongoConfig.database,
//     mongoUser: config.vehicleReactorMongoUser,
//     seq: seq
// }, { provider: config.k8sProvider })
