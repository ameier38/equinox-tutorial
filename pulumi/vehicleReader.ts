import * as docker from '@pulumi/docker'
import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'
import * as config from './config'
import { cosmicdealershipNamespace } from './namespace'
import { mongo } from './mongo'

type VehicleReaderArgs = {
    namespace: pulumi.Input<string>
    registryEndpoint: pulumi.Input<string>
    imageRegistry: pulumi.Input<docker.ImageRegistry>
    dockerCredentials: pulumi.Input<string>
    mongoHost: pulumi.Input<string>
    mongoPort: pulumi.Input<string>
    mongoDatabase: pulumi.Input<string>
    mongoUser: pulumi.Input<string>
    mongoPassword: pulumi.Input<string>
    seqHost: pulumi.Input<string>
    seqPort: pulumi.Input<string>
}

export class VehicleReader extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>

    constructor(name:string, args:VehicleReaderArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:VehicleReader', name, {}, opts)

        const registrySecret = new k8s.core.v1.Secret(`${name}-vehicle-reader-registry`, {
            metadata: { namespace: args.namespace },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.dockerCredentials
            }
        }, { parent: this })

        const mongoSecret = new k8s.core.v1.Secret(`${name}-vehicle-reader-mongo`, {
            metadata: { namespace: args.namespace },
            stringData: {
                user: args.mongoUser,
                password: args.mongoPassword
            }
        }, { parent: this })

        const image = new docker.Image(name, {
            imageName: pulumi.interpolate `${args.registryEndpoint}/cosmicdealership/${name}-vehicle-reader`,
            build: {
                context: path.join(config.root, 'vehicle'),
                dockerfile: path.join(config.root, 'vehicle', 'docker', 'reader.Dockerfile'),
                target: 'runner'
            },
            registry: args.imageRegistry
        }, { parent: this })

        const chartName = `${name}-vehicle-reader`
        const chart = new k8s.helm.v3.Chart(name, {
            chart: 'base-service',
            version: '0.1.2',
            fetchOpts: {
                repo: 'https://ameier38.github.io/charts'
            },
            namespace: args.namespace,
            values: {
                fullnameOverride: chartName,
                image: image.imageName,
                imagePullSecrets: [registrySecret.metadata.name],
                backendType: 'grpc',
                containerPort: 50051,
                env: {
                    MONGO_SECRET: mongoSecret.metadata.name,
                    MONGO_HOST: args.mongoHost,
                    MONGO_PORT: args.mongoPort,
                    MONGO_DATABASE: args.mongoDatabase,
                    SEQ_HOST: args.seqHost,
                    SEQ_PORT: args.seqPort
                },
                secrets: [
                    mongoSecret.metadata.name
                ]
            }
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, chartName, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalPort =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, chartName, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'grpc')!.port)

    }
}

// export const vehicleReader = new VehicleReader('v1', {
//     namespace: cosmicdealershipNamespace.metadata.name,
//     registryEndpoint: config.registryEndpoint,
//     imageRegistry: config.imageRegistry,
//     dockerCredentials: config.dockerCredentials,
//     mongoHost: mongo.internalHost,
//     mongoPort: mongo.internalPort.apply(p => `${p}`),
//     mongoDatabase: config.mongoConfig.database,
//     mongoUser: config.mongoReader.name,
//     mongoPassword: config.mongoReader.password,
//     seqHost: config.seqInternalHost,
//     seqPort: config.seqInternalPort.apply(p => `${p}`)
// }, { provider: config.k8sProvider })
