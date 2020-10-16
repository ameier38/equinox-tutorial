import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as config from './config'
import { Client } from './client'
import { dealershipNamespace } from './k8s'
import { externalGateway } from './gateway'
import { zone } from './cloudflare'

type EventStoreArgs = {
    chartVersion: pulumi.Input<string>
    namespace: k8s.core.v1.Namespace
    password: pulumi.Input<string>
}

export class EventStore extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>

    constructor(name:string, args:EventStoreArgs, opts:pulumi.ComponentResourceOptions) {
        super('infrastructure:EventStore', name, {}, opts)

        const chart = new k8s.helm.v3.Chart(name, {
            repo: 'eventstore',
            chart: 'eventstore',
            version: args.chartVersion,
            fetchOpts: {
                repo: 'https://ameier38.github.io/EventStore.Charts/'
            },
            namespace: args.namespace.metadata.name,
            values: {
                clusterSize: 3,
                persistence: {
                    enabled: true
                },
                admin: {
                    password: args.password
                },
                resources: {
                    requests: { cpu: '500m', memory: '500Mi' },
                    limits: { cpu: '1', memory: '500Mi' }
                }
            }
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-eventstore`, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalPort =
            pulumi.all([chart, args.namespace.metadata.name])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-eventstore`, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'ext-http-port')?.port)

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort
        })
    }
}

export const eventstore = new EventStore('dealership', {
    chartVersion: '0.2.5',
    namespace: dealershipNamespace,
    password: config.eventstoreConfig.password
}, { provider: config.k8sProvider })

export const eventstoreClient = new Client('eventstore', {
    namespace: dealershipNamespace,
    gateway: externalGateway,
    zone: zone,
    subdomain: 'eventstore',
    authUrl: config.auth0Config.domain,
    serviceHost: eventstore.internalHost,
    servicePort: eventstore.internalPort
}, { providers: [ config.k8sProvider, config.cloudflareProvider, config.auth0Provider ] })
