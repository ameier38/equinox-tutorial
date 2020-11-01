import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as config from './config'
import { cosmicdealershipNamespace } from './namespace'

function generateInitScript(endpoint:string, adminPassword:string, users:config.EventStoreUser[]) {
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
        `curl -i -s -f -u 'admin:${adminPassword}' -d '${data}' -H 'content-type:application/json' ${endpoint}/users/`
    ).join('\n')

    const defaultAcl = JSON.stringify([{
        // NB: update the event id you add or remove users
        // we don't generate in script because config map would change on every update
        // python -c 'import uuid; print(uuid.uuid4())'
        eventId: '4c8b034c-644c-446b-aa0e-f268d9ef8bd0',
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
                // NB: allow read from $all, etc.
                '$r': [adminsGroup, ...readUsers],
                '$w': adminsGroup,
                '$d': adminsGroup,
                '$mr': adminsGroup,
                '$mw': adminsGroup
            }
        }
    }])
    return `#!/bin/bash
set -e
# wait until gossip is available
while [[ "$(curl -s -o /dev/null -w '%{http_code}' ${endpoint}/gossip)" != "200" ]]; do 
    echo "waiting for Event Store at ${endpoint}..."
    sleep 5
done
# check if the password is already set to make this operation idempotent
if [[ "$(curl -s -u 'admin:${adminPassword}' -o /dev/null -w '%{http_code}' ${endpoint}/users)" = "200" ]]; then
    echo "Password already set"
else
    echo "Setting password"
    # ref: https://eventstore.org/docs/http-api/swagger/Users/Reset%20password.html
    curl \
        -i \
        -s \
        -f \
        -u 'admin:changeit' \
        -H 'content-type:application/json' \
        -d '{"newPassword":"${adminPassword}"}' \
        ${endpoint}/users/admin/command/reset-password
fi
sleep 5
echo 'creating users...'
${createUsers}
echo 'updating default acl...'
curl \
    -i \
    -s \
    -f \
    -u 'admin:${adminPassword}' \
    -H 'content-type:application/vnd.eventstore.events+json' \
    -d '${defaultAcl}' \
    ${endpoint}/streams/%24settings
echo 'init complete'
`
}

type EventStoreArgs = {
    chartVersion: pulumi.Input<string>
    namespace: pulumi.Input<string>
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
                repo: 'https://ameier38.github.io/charts'
            },
            namespace: args.namespace,
            values: {
                clusterSize: 1,
                persistence: {
                    enabled: true
                },
                eventStoreConfig: {
                    EVENTSTORE_GOSSIP_ON_SINGLE_NODE: 'True'
                },
                resources: {
                    requests: { cpu: '250m', memory: '500Mi' },
                    limits: { cpu: '500m', memory: '500Mi' }
                }
            }
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-eventstore`, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalPort =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-eventstore`, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'ext-http-port')!.port)

        const endpoint = pulumi.interpolate `http://${this.internalHost}:${this.internalPort}`

        const initScript = pulumi
            .all([endpoint, args.adminPassword])
            .apply(([endpoint, adminPassword]) =>
                generateInitScript(endpoint, adminPassword, args.users))

        const initConfigMap = new k8s.core.v1.ConfigMap(`${name}-init-eventstore`, {
            metadata: { namespace: args.namespace },
            data: {
                'init.sh': initScript
            }
        }, { parent: this })

        new k8s.batch.v1.Job(`${name}-init-eventstore`, {
            metadata: { namespace: args.namespace },
            spec: {
                backoffLimit: 2,
                ttlSecondsAfterFinished: 100,
                template: {
                    spec: {
                        restartPolicy: 'Never',
                        containers: [{
                            name: 'init',
                            image: 'tutum/curl',
                            command: ['/usr/local/scripts/init.sh'],
                            volumeMounts: [{
                                name: 'init',
                                mountPath: '/usr/local/scripts',
                            }]
                        }],
                        volumes: [{
                            name: 'init',
                            configMap: { name: initConfigMap.metadata.name, defaultMode: 484 },
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

export const eventstore = new EventStore(config.env, {
    chartVersion: '0.1.0',
    namespace: cosmicdealershipNamespace.metadata.name,
    adminPassword: config.eventstoreConfig.adminPassword,
    users: config.eventstoreConfig.users
}, { provider: config.k8sProvider })
