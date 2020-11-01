import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as config from './config'
import { cosmicdealershipNamespace } from './namespace'

function generateInitScript(rootPassword:string, users:config.MongoUser[]) {
    const createUsers = users.map(user => {
        const permissions = user.permissions.map(permission => {
            return `{ role: "${permission.role}", db: "${permission.database}" }`
        }).join(',')
        return `
db.createUser(
  {
    user: "${user.name}",
    pwd: "${user.password}",
    roles: [${permissions}]
  }
)
`
    }).join('\n')

    return `#!/bin/bash
set -e
mongo -u root -p '${rootPassword}' << EOF
use admin
${createUsers}
EOF
`
}

type MongoArgs = {
    chartVersion: pulumi.Input<string>
    namespace: pulumi.Input<string>
    rootPassword: pulumi.Input<string>
    replicaSetName: pulumi.Input<string>
    users: config.MongoUser[]
}

export class Mongo extends pulumi.ComponentResource {
    internalHost: pulumi.Output<string>
    internalPort: pulumi.Output<number>

    constructor(name:string, args:MongoArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:Mongo', name, {}, opts)

        const chart = new k8s.helm.v3.Chart(name, {
            chart: 'mongodb',
            fetchOpts: {
                repo: 'https://charts.bitnami.com/bitnami'
            },
            namespace: args.namespace,
            values: {
                architecture: 'standalone',
                auth: {
                    enabled: true,
                    rootPassword: args.rootPassword
                },
                extraEnvVars: [
                    {name: 'MONGODB_REPLICA_SET_MODE', value: 'primary'},
                    {name: 'MONGODB_REPLICA_SET_NAME', value: 'rs0'},
                    {name: 'MONGODB_REPLICA_SET_KEY', value: 'changeit'}
                ],
                initdbScripts: {
                    'init.sh': pulumi.output(args.rootPassword).apply(rootPassword => generateInitScript(rootPassword, args.users)) 
                }
            }
        }, { parent: this })

        this.internalHost =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-mongodb`, 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        this.internalPort =
            pulumi.all([chart, args.namespace])
            .apply(([chart, namespace]) => chart.getResourceProperty('v1/Service', namespace, `${name}-mongodb`, 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'mongodb')!.port)

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort
        })
    }
}

export const mongo = new Mongo(config.env, {
    chartVersion: '9.2.5',
    namespace: cosmicdealershipNamespace.metadata.name,
    rootPassword: config.mongoConfig.rootPassword,
    replicaSetName: config.mongoConfig.replicaSetName,
    users: config.mongoConfig.users
}, { provider: config.k8sProvider })
