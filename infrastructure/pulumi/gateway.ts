import * as pulumi from '@pulumi/pulumi'
import * as k8s from '@pulumi/kubernetes'
import * as config from './config'
import { infrastructureNamespace } from './k8s'

type GatewayArgs = {
    chartVersion: pulumi.Input<string>
    namespace: k8s.core.v1.Namespace
}

export class Gateway extends pulumi.ComponentResource {
    serviceHost: pulumi.Output<string>
    servicePort: pulumi.Output<number>
    loadBalancerHost: pulumi.Output<string>

    constructor(name:string, args:GatewayArgs, opts:pulumi.ComponentResourceOptions) {
        super('infrastructure:Gateway', name, {}, opts)

        const chart = new k8s.helm.v3.Chart(name, {
            repo: 'datawire',
            chart: 'ambassador',
            fetchOpts: {
                repo: 'https://getambassador.io'
            },
            version: args.chartVersion,
            namespace: args.namespace.metadata.name,
            values: {
                test: {
                    enabled: false
                },
                crds: {
                    enabled: true,
                    create: true,
                    keep: false
                },
                replicaCount: 1,
                adminService: {
                    create: false
                }
            }
        }, { parent: this })

        this.serviceHost =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-ambassador`, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.servicePort =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-ambassador`, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'http')?.port)

        this.loadBalancerHost =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) =>
                chart.getResourceProperty('v1/Service', namespace, `${name}-ambassador`, 'status')
                .apply(status => status.loadBalancer.ingress[0].hostname))
    }
}

export const externalGateway = new Gateway('external', {
    chartVersion: '6.5.9',
    namespace: infrastructureNamespace
}, {provider: config.k8sProvider })
