import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as config from './config'
import { infrastructureNamespace, dealershipNamespace } from './k8s'

type MongoArgs = {
    chartVersion: pulumi.Input<string>
    namespace: k8s.core.v1.Namespace
    user: pulumi.Input<string>
    password: pulumi.Input<string>
}

export class Mongo extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>

    constructor(name:string, args:MongoArgs, opts:pulumi.ComponentResourceOptions) {
        super('infrastructure:Mongo', name, {}, opts)

        const chart = new k8s.helm.v3.Chart(name, {
            repo: 'bitnami',
            chart: 'mongodb',
            fetchOpts: {
                repo: 'https://charts.bitnami.com/bitnami'
            },
            namespace: args.namespace.metadata.name,
            values: {
                architecture: 'replicaset',
                auth: {
                    enabled: true,
                    rootPassword: args.password,
                    username: args.user,
                    password: args.password
                }
            }
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-mongodb`, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalPort =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-mongodb`, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'mongodb')?.port)

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort
        })
    }
}

export const mongo = new Mongo('dealership', {
    chartVersion: '9.2.4',
    namespace: infrastructureNamespace,
    user: config.mongoConfig.user,
    password: config.mongoConfig.password
}, { provider: config.k8sProvider })
