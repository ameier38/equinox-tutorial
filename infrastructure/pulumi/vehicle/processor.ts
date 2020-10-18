import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'
import * as config from '../config'
import { EventStore, eventstore } from '../eventstore'
import { Seq } from '../seq'
import { Registry, registry } from '../registry'
import { dealershipNamespace } from '../k8s'
import { Service } from '../service'

type VehicleProcessorArgs = {
    namespace: k8s.core.v1.Namespace
    registry: Registry
    eventstore: EventStore
    eventstoreUser: config.EventStoreUser
    seq: Seq
}

export class VehicleProcessor extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>

    constructor(name:string, args:VehicleProcessorArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:VehicleProcessor', name, {}, opts)

        const registrySecret = new k8s.core.v1.Secret(`${name}-vehicle-processor-registry`, {
            metadata: { namespace: args.namespace.metadata.name },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.registry.dockerCredentials
            }
        }, { parent: this })

        const eventstoreSecret = new k8s.core.v1.Secret(`${name}-vehicle-processor-eventstore`, {
            metadata: { namespace: args.namespace.metadata.name },
            stringData: {
                user: args.eventstoreUser.name,
                password: args.eventstoreUser.password
            }
        }, { parent: this })

        const service = new Service('vehicle-processor', {
            namespace: args.namespace,
            registry: args.registry,
            context: path.join(config.root, 'vehicle'),
            dockerfile: path.join(config.root, 'vehicle', 'deploy', 'processor.Dockerfile'),
            buildTarget: 'runner',
            buildArgs: {},
            backendType: 'grpc',
            imagePullSecret: registrySecret,
            secrets: [ eventstoreSecret ],
            env: {
                EVENTSTORE_SECRET: eventstoreSecret.metadata.name,
                EVENTSTORE_SCHEME: 'discover',
                EVENTSTORE_HOST: args.eventstore.internalHost,
                EVENTSTORE_PORT: args.eventstore.internalPort.apply(port => `${port}`),
                SEQ_SCHEME: 'http',
                SEQ_HOST: args.seq.internalHost,
                SEQ_PORT: args.seq.internalIngestionPort.apply(port => `${port}`)
            }
        }, { parent: this })

        this.internalHost = service.internalHost!
        this.internalPort = service.internalPort!

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort
        })
    }
}

// export const vehicleProcessor = new VehicleProcessor('v1', {
//     namespace: dealershipNamespace,
//     eventstore: eventstore,
//     eventstoreUser: config.vehicleProcessorEventStoreUser,
//     registry: registry,
//     seq: seq
// }, { provider: config.k8sProvider })
