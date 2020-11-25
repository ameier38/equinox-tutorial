import * as cloudflare from '@pulumi/cloudflare'
import * as docker from '@pulumi/docker'
import * as pulumi from '@pulumi/pulumi'
import * as k8s from '@pulumi/kubernetes'
import * as path from 'path'
import * as config from './config'
import { cosmicdealershipNamespace } from './namespace'
import { zone } from './zone'

type HandbookArgs = {
    zoneId: pulumi.Input<string>
    acmeEmail: pulumi.Input<string>
    namespace: pulumi.Input<string>
    registryEndpoint: pulumi.Input<string>
    imageRegistry: pulumi.Input<docker.ImageRegistry>
    dockerCredentials: pulumi.Input<string>
    loadBalancerAddress: pulumi.Input<string>
}

class Handbook extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>
    host: pulumi.Output<string>

    constructor(name:string, args:HandbookArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:Handbook', name, {}, opts)

        const identifier = `${name}-handbook`

        const record = new cloudflare.Record(identifier, {
            zoneId: args.zoneId,
            name: 'handbook',
            type: 'A',
            value: args.loadBalancerAddress
        }, { parent: this })

        this.host = record.hostname

        const registrySecret = new k8s.core.v1.Secret(`${identifier}-registry`, {
            metadata: { namespace: args.namespace },
            type: 'kubernetes.io/dockerconfigjson',
            stringData: {
                '.dockerconfigjson': args.dockerCredentials
            }
        }, { parent: this })

        const image = new docker.Image(name, {
            imageName: pulumi.interpolate `${args.registryEndpoint}/cosmicdealership/${identifier}`,
            build: {
                context: path.join(config.root, 'handbook'),
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
                backendType: 'http',
                containerPort: 8000
            }
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, identifier, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalPort =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, identifier, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'http')!.port)

        // NB: generates certificate
        new k8s.apiextensions.CustomResource(identifier, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Host',
            metadata: { namespace: args.namespace },
            spec: {
                hostname: this.host,
                acmeProvider: {
                    email: args.acmeEmail
                }
            }
        }, { parent: this })

        // NB: specifies how to direct incoming requests
        new k8s.apiextensions.CustomResource(identifier, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Mapping',
            metadata: { namespace: args.namespace },
            spec: {
                prefix: '/',
                host: this.host,
                service: pulumi.interpolate `${this.internalHost}:${this.internalPort}`
            }
        }, { parent: this })

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort,
            host: this.host
        })
    }
}

// export const handbook = new Handbook(config.env, {
//     namespace: cosmicdealershipNamespace.metadata.name,
//     registryEndpoint: config.registryEndpoint,
//     imageRegistry: config.imageRegistry,
//     dockerCredentials: config.dockerCredentials,
//     acmeEmail: config.acmeEmail,
//     zoneId: zone.id,
//     loadBalancerAddress: config.loadBalancerAddress
// }, { providers: [ config.k8sProvider, config.cloudflareProvider ]})
