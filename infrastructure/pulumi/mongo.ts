import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as config from './config'
import { infrastructureNamespace } from './k8s'

function generateInitdbScript(users:config.MongoUser[]) {
    const scripts = users.map(user => {
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
    return scripts
}

type MongoArgs = {
    chartVersion: pulumi.Input<string>
    namespace: k8s.core.v1.Namespace
    adminUser: pulumi.Input<string>
    adminPassword: pulumi.Input<string>
    database: pulumi.Input<string>
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
            namespace: args.namespace.metadata.name,
            values: {
                architecture: 'replicaset',
                auth: {
                    enabled: true,
                    rootPassword: args.adminPassword,
                    username: args.adminUser,
                    password: args.adminPassword,
                    database: args.database
                },
                initdbScripts: {
                    'createUsers.js': generateInitdbScript(args.users)
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
            .apply(spec => spec.ports.find(port => port.name === 'mongodb')!.port)

        this.registerOutputs({
            internalHost: this.internalHost,
            internalPort: this.internalPort
        })
    }
}

export const mongo = new Mongo('dealership', {
    chartVersion: '9.2.4',
    namespace: infrastructureNamespace,
    adminUser: config.mongoConfig.adminUser,
    adminPassword: config.mongoConfig.adminPassword,
    database: config.mongoConfig.database,
    users: config.mongoConfig.users
}, { provider: config.k8sProvider })
