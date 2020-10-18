import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as config from './config'
import { monitoringNamespace } from './k8s'

type SeqArgs = {
    chartVersion: pulumi.Input<string>
    namespace: k8s.core.v1.Namespace
}

export class Seq extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalIngestionPort: pulumi.Output<number>
    internalUiPort: pulumi.Output<number>

    constructor(name:string, args:SeqArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:Seq', name, {}, opts)

        const chart = new k8s.helm.v3.Chart(name, {
            chart: 'seq',
            version: args.chartVersion,
            fetchOpts: {
                repo: 'https://kubernetes-charts.storage.googleapis.com/'
            },
            namespace: args.namespace.metadata.name
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-seq`, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalIngestionPort =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-seq`, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'ingestion')!.port)

        this.internalUiPort =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-seq`, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'ui')!.port)

        this.registerOutputs({
            internalHost: this.internalHost,
            internalIngestionPort: this.internalIngestionPort,
            internalUiPort: this.internalUiPort
        })
    }
}

// export const seq = new Seq('dealership', {
//     chartVersion: '2.3.0',
//     namespace: monitoringNamespace,
// }, { provider: config.k8sProvider })
