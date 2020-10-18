import * as pulumi from '@pulumi/pulumi'
import * as k8s from '@pulumi/kubernetes'
import * as docker from '@pulumi/docker'
import * as path from 'path'
import * as config from './config'
import { Registry } from './registry'

type BackendType = 'grpc' | 'http' | 'console'

type ServiceArgs = {
    context: pulumi.Input<string>
    backendType: BackendType
    dockerfile: pulumi.Input<string>
    buildTarget: pulumi.Input<string>
    buildArgs: Record<string,pulumi.Input<string>>
    namespace: k8s.core.v1.Namespace
    registry: Registry
    imagePullSecret: k8s.core.v1.Secret
    secrets: k8s.core.v1.Secret[]
    env: Record<string,pulumi.Input<string>>
}

export class Service extends pulumi.ComponentResource {
    internalHost?: pulumi.Output<string>
    internalPort?: pulumi.Output<number>

    constructor(name:string, args:ServiceArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:Service', name, {}, opts)

        const image = new docker.Image(name, {
            imageName: pulumi.interpolate `${args.registry.imageRegistry.server}/${name}`,
            build: {
                context: args.context,
                dockerfile: args.dockerfile,
                target: args.buildTarget,
                args: args.buildArgs
            },
            registry: args.registry.imageRegistry
        }, { parent: this })

        const chart = new k8s.helm.v3.Chart(name, {
            path: path.join(config.root, 'infrastructure', 'helm', 'base-service'),
            namespace: args.namespace.metadata.name,
            values: {
                fullnameOverride: name,
                image: image.imageName,
                imagePullSecrets: [args.imagePullSecret.metadata.name],
                backendType: args.backendType,
                env: args.env,
                secrets: args.secrets.map(secret => secret.metadata.name)
            }
        }, { parent: this })

        if (['grpc', 'http'].includes(args.backendType)) {
            this.internalHost =
                pulumi.all([chart, args.namespace.metadata.name])
                .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, name, 'metadata'))
                .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

            this.internalPort =
                pulumi.all([chart, args.namespace.metadata.name])
                .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, name, 'spec'))
                .apply(spec => spec.ports.find(port => port.name === args.backendType)!.port)
        }

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort
        })
    }
}
