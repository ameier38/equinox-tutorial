import * as pulumi from '@pulumi/pulumi'
import * as k8s from '@pulumi/kubernetes'
import * as path from 'path'
import * as config from '../config'
import { Mongo } from '../mongo'
import { Seq } from '../seq'
import { Registry, registry } from '../registry'
import { dealershipNamespace } from '../k8s'
import { Service } from '../service'

type VehicleReaderArgs = {
    namespace: k8s.core.v1.Namespace
    registry: Registry
    mongo: Mongo
    mongoDatabase: pulumi.Input<string>
    mongoUser: config.MongoUser
    seq: Seq
}

export class VehicleReader extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>

    constructor(name:string, args:VehicleReaderArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:VehicleReader', name, {}, opts)

        const registrySecret = new k8s.core.v1.Secret(`${name}-vehicle-reader-registry`, {
            metadata: { namespace: args.namespace.metadata.name },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.registry.dockerCredentials
            }
        }, { parent: this })

        const mongoSecret = new k8s.core.v1.Secret(`${name}-vehicle-reader-mongo`, {
            metadata: { namespace: args.namespace.metadata.name },
            stringData: {
                user: args.mongoUser.name,
                password: args.mongoUser.password
            }
        }, { parent: this })

        const service = new Service('vehicle-reader', {
            namespace: dealershipNamespace,
            registry: registry,
            context: path.join(config.root, 'vehicle'),
            dockerfile: path.join(config.root, 'vehicle', 'deploy', 'reader.Dockerfile'),
            buildTarget: 'runner',
            buildArgs: {},
            backendType: 'grpc',
            imagePullSecret: registrySecret,
            secrets: [ mongoSecret ],
            env: {
                MONGO_SECRET: mongoSecret.metadata.name,
                MONGO_HOST: args.mongo.internalHost,
                MONGO_PORT: args.mongo.internalPort.apply(port => `${port}`),
                MONGO_DATABASE: args.mongoDatabase,
                SEQ_SCHEME: 'http',
                SEQ_HOST: args.seq.internalHost,
                SEQ_PORT: args.seq.internalIngestionPort.apply(port => `${port}`)
            }
        }, { parent: this })

        this.internalHost = service.internalHost!
        this.internalPort = service.internalPort!
    }
}

// export const vehicleReader = new VehicleReader('v1', {
//     namespace: dealershipNamespace,
//     registry: registry,
//     mongo: mongo,
//     mongoDatabase: config.mongoConfig.database,
//     mongoUser: config.vehicleReaderMongoUser,
//     seq: seq
// }, { provider: config.k8sProvider })
