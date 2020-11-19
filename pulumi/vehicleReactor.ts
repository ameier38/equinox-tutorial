import * as docker from '@pulumi/docker'
import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'
import * as config from './config'
import { cosmicdealershipNamespace } from './namespace'
import { eventstore } from './eventstore'
import { mongo } from './mongo'

type VehicleReactorArgs = {
    namespace: pulumi.Input<string>
    registryEndpoint: pulumi.Input<string>
    imageRegistry: pulumi.Input<docker.ImageRegistry>
    dockerCredentials: pulumi.Input<string>
    eventstoreHost: pulumi.Input<string>
    eventstorePort: pulumi.Input<string>
    eventstoreUser: pulumi.Input<string>
    eventstorePassword: pulumi.Input<string>
    mongoHost: pulumi.Input<string>
    mongoPort: pulumi.Input<string>
    mongoDatabase: pulumi.Input<string>
    mongoUser: pulumi.Input<string>
    mongoPassword: pulumi.Input<string>
    seqHost: pulumi.Input<string>
    seqPort: pulumi.Input<string>
}

export class VehicleReactor extends pulumi.ComponentResource {
    constructor(name:string, args:VehicleReactorArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:VehicleReactor', name, {}, opts)

        const identifier = `${name}-vehicle-reactor`

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

        const mongoSecret = new k8s.core.v1.Secret(`${identifier}-mongo`, {
            metadata: { namespace: args.namespace },
            stringData: {
                user: args.mongoUser,
                password: args.mongoPassword
            }
        }, { parent: this })

        const image = new docker.Image(name, {
            imageName: pulumi.interpolate `${args.registryEndpoint}/cosmicdealership/${identifier}`,
            build: {
                context: path.join(config.root, 'vehicle'),
                dockerfile: path.join(config.root, 'vehicle', 'docker', 'reactor.Dockerfile'),
                target: 'runner',
                env: { DOCKER_BUILDKIT: '1' }
            },
            registry: args.imageRegistry
        }, { parent: this })

        new k8s.helm.v3.Chart(identifier, {
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
                backendType: 'console',
                env: {
                    DEBUG: 'true',
                    EVENTSTORE_SECRET: eventstoreSecret.metadata.name,
                    EVENTSTORE_SCHEME: 'discover',
                    EVENTSTORE_HOST: args.eventstoreHost,
                    EVENTSTORE_PORT: args.eventstorePort,
                    MONGO_SECRET: mongoSecret.metadata.name,
                    MONGO_HOST: args.mongoHost,
                    MONGO_PORT: args.mongoPort,
                    MONGO_DATABASE: args.mongoDatabase,
                    SEQ_HOST: args.seqHost,
                    SEQ_PORT: args.seqPort
                },
                secrets: [
                    eventstoreSecret.metadata.name,
                    mongoSecret.metadata.name
                ]
            }
        }, { parent: this })
    }
}

export const vehicleReactor = new VehicleReactor(config.env, {
    namespace: cosmicdealershipNamespace.metadata.name,
    registryEndpoint: config.registryEndpoint,
    imageRegistry: config.imageRegistry,
    dockerCredentials: config.dockerCredentials,
    eventstoreHost: eventstore.internalHost,
    eventstorePort: eventstore.internalPort.apply(p => `${p}`),
    eventstoreUser: config.eventstoreReader.name,
    eventstorePassword: config.eventstoreReader.password,
    mongoHost: mongo.internalHost,
    mongoPort: mongo.internalPort.apply(p => `${p}`),
    mongoDatabase: config.mongoConfig.database,
    mongoUser: config.mongoWriter.name,
    mongoPassword: config.mongoWriter.password,
    seqHost: config.seqInternalHost,
    seqPort: config.seqInternalPort.apply(p => `${p}`)
}, { provider: config.k8sProvider })
