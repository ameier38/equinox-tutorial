import * as docker from '@pulumi/docker'
import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'
import * as config from './config'
import { cosmicdealershipNamespace } from './namespace'
import { eventstore } from './eventstore'

type VehicleProcessorArgs = {
    namespace: pulumi.Input<string>
    registryEndpoint: pulumi.Input<string>
    imageRegistry: pulumi.Input<docker.ImageRegistry>
    dockerCredentials: pulumi.Input<string>
    eventstoreHost: pulumi.Input<string>
    eventstorePort: pulumi.Input<string>
    eventstoreUser: pulumi.Input<string>
    eventstorePassword: pulumi.Input<string>
    seqHost: pulumi.Input<string>
    seqPort: pulumi.Input<string>
}

export class VehicleProcessor extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>

    constructor(name:string, args:VehicleProcessorArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:VehicleProcessor', name, {}, opts)

        const identifier = `${name}-vehicle-processor`

        const registrySecret = new k8s.core.v1.Secret(`${identifier}-registry`, {
            metadata: { namespace: args.namespace },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.dockerCredentials
            }
        }, { parent: this })

        const eventstoreSecret = new k8s.core.v1.Secret(`${identifier}-eventstore`, {
            metadata: { namespace: args.namespace },
            stringData: {
                user: args.eventstoreUser,
                password: args.eventstorePassword
            }
        }, { parent: this })

        const image = new docker.Image(name, {
            imageName: pulumi.interpolate `${args.registryEndpoint}/cosmicdealership/${identifier}`,
            build: {
                context: path.join(config.root, 'vehicle'),
                dockerfile: path.join(config.root, 'vehicle', 'docker', 'processor.Dockerfile'),
                target: 'runner',
                env: { DOCKER_BUILDKIT: '1' }
            },
            registry: args.imageRegistry
        }, { parent: this })

        const chart = new k8s.helm.v3.Chart(identifier, {
            chart: 'base-service',
            version: '0.1.2',
            fetchOpts: {
                repo: 'https://ameier38.github.io/charts'
            },
            namespace: args.namespace,
            values: {
                nameOverride: identifier,
                fullnameOverride: identifier,
                image: image.imageName,
                imagePullSecrets: [registrySecret.metadata.name],
                backendType: 'grpc',
                containerPort: 50051,
                env: {
                    DEBUG: 'true',
                    EVENTSTORE_SECRET: eventstoreSecret.metadata.name,
                    EVENTSTORE_SCHEME: 'discover',
                    EVENTSTORE_HOST: args.eventstoreHost,
                    EVENTSTORE_PORT: args.eventstorePort,
                    SEQ_HOST: args.seqHost,
                    SEQ_PORT: args.seqPort
                },
                secrets: [
                    eventstoreSecret.metadata.name
                ]
            }
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, identifier, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalPort =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, identifier, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'grpc')!.port)


        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort
        })
    }
}

export const vehicleProcessor = new VehicleProcessor(config.env, {
    namespace: cosmicdealershipNamespace.metadata.name,
    registryEndpoint: config.registryEndpoint,
    imageRegistry: config.imageRegistry,
    dockerCredentials: config.dockerCredentials,
    eventstoreHost: eventstore.internalHost,
    eventstorePort: eventstore.internalPort.apply(p => `${p}`),
    eventstoreUser: config.eventstoreWriter.name,
    eventstorePassword: config.eventstoreWriter.password,
    seqHost: config.seqInternalHost,
    seqPort: config.seqInternalPort.apply(p => `${p}`)
}, { provider: config.k8sProvider })