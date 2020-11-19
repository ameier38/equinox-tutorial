import * as auth0 from '@pulumi/auth0'
import * as cloudflare from '@pulumi/cloudflare'
import * as docker from '@pulumi/docker'
import * as digitalocean from '@pulumi/digitalocean'
import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'

export const root = path.dirname(__dirname)

export const env = pulumi.getStack()

export const zone = 'cosmicdealership.com'

export const oauthAudience = `https://${zone}`

const infrastructureStack = new pulumi.StackReference(`${env}-infrastructure-stack`, {
    name: `ameier38/infrastructure/${env}`
})

export const acmeEmail = infrastructureStack.requireOutput('acmeEmail').apply(o => o as string)
export const registryEndpoint = infrastructureStack.requireOutput('registryEndpoint').apply(o => o as string)
export const imageRegistry = infrastructureStack.requireOutput('imageRegistry').apply(o => o as docker.ImageRegistry)
export const dockerCredentials = infrastructureStack.requireOutput('dockerCredentials').apply(o => o as string)
export const loadBalancerAddress = infrastructureStack.requireOutput('loadBalancerAddress').apply(o => o as string)
export const seqInternalHost = infrastructureStack.requireOutput('seqInternalHost').apply(o => o as string)
export const seqInternalPort = infrastructureStack.requireOutput('seqInternalPort').apply(o => o as number)
const kubeconfig = infrastructureStack.requireOutput('kubeconfig').apply(o => o as string)

const rawDigitalOceanConfig = new pulumi.Config('digitalocean')
export const digitalOceanProvider = new digitalocean.Provider(`${env}-digitalocean-provider`, {
    token: rawDigitalOceanConfig.require('token'),
    spacesEndpoint: `https://${digitalocean.Regions.NYC3}.digitaloceanspaces.com`,
    spacesAccessId: rawDigitalOceanConfig.require('spacesAccessId'),
    spacesSecretKey: rawDigitalOceanConfig.require('spacesSecretKey')
})


export const k8sProvider = new k8s.Provider(`${env}-k8s-provider`, {
    kubeconfig: kubeconfig
})

export const gatewayConfig = {
    loadBalancerAddress: infrastructureStack.requireOutput('loadBalancerAddress'),
}

const rawCloudflareConfig = new pulumi.Config('cloudflare')
export const cloudflareProvider = new cloudflare.Provider(`${env}-cloudflare-provider`, {
    email: rawCloudflareConfig.require('email'),
    apiKey: rawCloudflareConfig.require('apiKey')
})

const rawAuth0Config = new pulumi.Config('auth0')
export const auth0Config = {
    domain: rawAuth0Config.require('domain'),
    clientId: rawAuth0Config.require('clientId'),
    clientSecret: rawAuth0Config.require('clientSecret'),
    testAdminEmail: rawAuth0Config.require('testAdminEmail'),
    testAdminPassword: rawAuth0Config.require('testAdminPassword'),
    testCustomerEmail: rawAuth0Config.require('testCustomerEmail'),
    testCustomerPassword: rawAuth0Config.require('testCustomerPassword')
}
export const oauthIssuer = pulumi.interpolate `https://${auth0Config.domain}`
export const auth0Provider = new auth0.Provider(`${env}-auth0-provider`, {
    domain: auth0Config.domain,
    clientId: auth0Config.clientId,
    clientSecret: auth0Config.clientSecret
})

type EventStoreRole = 'read' | 'readWrite'

export type EventStoreUser = {
    name: string
    password: string
    role: EventStoreRole
}

const rawEventstoreConfig = new pulumi.Config('eventstore')

export const eventstoreWriter: EventStoreUser = {
    name: 'writer',
    password: rawEventstoreConfig.require('writerPassword'),
    role: 'readWrite'
}

export const eventstoreReader: EventStoreUser = {
    name: 'reader',
    password: rawEventstoreConfig.require('readerPassword'),
    role: 'read'
}

export const eventstoreConfig = {
    adminPassword: rawEventstoreConfig.require('adminPassword'),
    users: [eventstoreWriter, eventstoreReader]
}

type MongoRole = 'read' | 'readWrite'

type MongoPermission = {
    role: MongoRole
    database: string
}

export type MongoUser = {
    name: string
    password: string
    permissions: MongoPermission[] 
}

const rawMongoConfig = new pulumi.Config('mongo')


export const mongoWriter: MongoUser = {
    name: 'writer',
    password: rawMongoConfig.require('writerPassword'),
    permissions: [{role: 'readWrite', database: env}]
}

export const mongoReader: MongoUser = {
    name: 'reader',
    password: rawMongoConfig.require('readerPassword'),
    permissions: [{role: 'read', database: env}]
}

export const mongoConfig = {
    rootPassword: rawMongoConfig.require('rootPassword'),
    replicaSetName: rawMongoConfig.require('replicaSetName'),
    database: env,
    users: [mongoWriter, mongoReader]
}
