import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import { v4 as uuidv4 } from 'uuid'
import * as config from './config'
import { infrastructureNamespace } from './k8s'

function generateInitdbScript(endpoint:string, adminPassword:string, users:config.EventStoreUser[]) {
    const adminsGroup = '$admins'
    const systemUsers = ['$admin', '$ops']
    const readUsers = users.filter(user => ['read', 'readWrite'].includes(user.role)).map(user => user.name)
    const writeUsers = users.filter(user => ['readWrite'].includes(user.role)).map(user => user.name)

    const createUsers = users.map(user =>
        JSON.stringify({
            loginName: user.name,
            password: user.password
        })
    ).map(data => 
        `curl -f ${endpoint}/users/ -u admin:${adminPassword} -d '${data}' -H 'content-type:application/json'`
    ).join('\n')

    const defaultAcl = JSON.stringify([{
        eventId: uuidv4(),
        eventType: 'update-default-acl',
        data: {
            '$userStreamAcl': {
                '$r': [...systemUsers, ...readUsers],
                '$w': [...systemUsers, ...writeUsers],
                '$d': systemUsers,
                '$mr': systemUsers,
                '$mw': systemUsers,
            },
            '$systemStreamAcl': {
                '$r': adminsGroup,
                '$w': adminsGroup,
                '$d': adminsGroup,
                '$mr': adminsGroup,
                '$mw': adminsGroup
            }
        }
    }])
    return `#!/bin/bash
set -e

echo 'creating users...'
${createUsers}
echo 'done creating users'
echo 'updating default acl...'
curl \
    -f \
    -u admin:${adminPassword} \
    -H 'content-type:application/vnd.eventstore.events+json' \
    -d '${defaultAcl}' ${endpoint}/streams/%24settings
echo 'done updating default acl'
echo 'initdb completed'
`
}

type EventStoreArgs = {
    chartVersion: pulumi.Input<string>
    namespace: k8s.core.v1.Namespace
    adminPassword: pulumi.Input<string>
    users: config.EventStoreUser[]
}

export class EventStore extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>

    constructor(name:string, args:EventStoreArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:EventStore', name, {}, opts)

        const chart = new k8s.helm.v3.Chart(name, {
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
                    password: args.adminPassword
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
            .apply(spec => spec.ports.find(port => port.name === 'ext-http-port')!.port)


        const endpoint = pulumi.interpolate `http://${this.internalHost}:${this.internalPort}`

        const initdbScript = pulumi
            .all([endpoint, args.adminPassword])
            .apply(([endpoint, adminPassword]) =>
                generateInitdbScript(endpoint, adminPassword, args.users))

        const initdbConfigMap = new k8s.core.v1.ConfigMap(`${name}-initdb`, {
            metadata: { namespace: args.namespace.metadata.name },
            data: {
                'initdb.sh': initdbScript
            }
        }, { parent: this })

        new k8s.batch.v1.Job(`${name}-initdb`, {
            metadata: { namespace: args.namespace.metadata.name },
            spec: {
                backoffLimit: 2,
                ttlSecondsAfterFinished: 100,
                template: {
                    spec: {
                        restartPolicy: 'Never',
                        containers: [{
                            name: 'initdb',
                            image: 'tutum/curl',
                            command: ['/usr/local/scripts/initdb.sh'],
                            volumeMounts: [{
                                name: 'initdb',
                                mountPath: '/usr/local/scripts',
                            }]
                        }],
                        volumes: [{
                            name: 'initdb',
                            configMap: { name: initdbConfigMap.metadata.name, defaultMode: 484 },
                        }]
                    }
                }
            }
        }, { parent: this })

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort,
        })
    }
}

export const eventstore = new EventStore('dealership', {
    chartVersion: '0.2.5',
    namespace: infrastructureNamespace,
    adminPassword: config.eventstoreConfig.adminPassword,
    users: [config.vehicleProcessorEventStoreUser, config.vehicleReactorEventStoreUser]
}, { provider: config.k8sProvider })
