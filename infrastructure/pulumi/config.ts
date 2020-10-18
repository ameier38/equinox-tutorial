import * as auth0 from '@pulumi/auth0'
import * as cloudflare from '@pulumi/cloudflare'
import * as digitalocean from '@pulumi/digitalocean'
import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import * as path from 'path'

export const root = path.dirname(path.dirname(__dirname))

export const env = pulumi.getStack()

const infrastructureStack = new pulumi.StackReference(`${env}-infrastructure-stack`, {
    name: `ameier38/infrastructure/${env}`
})

const rawDnsConfig = new pulumi.Config('dns')
export const dnsConfig = {
    zone: rawDnsConfig.require('zone')
}

export const audience = `https://${dnsConfig.zone}`

const rawCloudflareConfig = new pulumi.Config('cloudflare')
export const cloudflareProvider = new cloudflare.Provider(`${env}-cloudflare-provider`, {
    email: rawCloudflareConfig.require('email'),
    apiKey: rawCloudflareConfig.require('apiKey')
})

const rawDigitalOceanConfig = new pulumi.Config('digitalocean')
export const digitalOceanProvider = new digitalocean.Provider(`${env}-digitalocean-provider`, {
    apiEndpoint: 'api.digitalocean.com/v2',
    token: rawDigitalOceanConfig.require('token'),
    spacesEndpoint: `${digitalocean.Regions.NYC3}.digitaloceanspaces.com`,
    spacesAccessId: rawDigitalOceanConfig.require('spacesAccessId'),
    spacesSecretKey: rawDigitalOceanConfig.require('spacesSecretKey')
})

export const k8sProvider = new k8s.Provider(`${env}-k8s-provider`, {
    kubeconfig: infrastructureStack.requireOutput('kubeconfig')
})

const rawAuth0Config = new pulumi.Config('auth0')
export const authUrl = rawAuth0Config.require('domain')
export const auth0Provider = new auth0.Provider(`${env}-auth0-provider`, {
    domain: authUrl,
    clientId: rawAuth0Config.require('clientId'),
    clientSecret: rawAuth0Config.require('clientSecret')
})

type EventStoreRole = 'read' | 'readWrite'

export type EventStoreUser = {
    name: string
    password: string
    role: EventStoreRole
}

const rawEventstoreConfig = new pulumi.Config('eventstore')

export const vehicleProcessorEventStoreUser: EventStoreUser = {
    name: 'vehicle-processor',
    password: rawEventstoreConfig.require('vehicleProcessorPassword'),
    role: 'readWrite'
}

export const vehicleReactorEventStoreUser: EventStoreUser = {
    name: 'vehicle-reactor',
    password: rawEventstoreConfig.require('vehicleReactorPassword'),
    role: 'read'
}

export const eventstoreConfig = {
    adminPassword: rawEventstoreConfig.require('adminPassword'),
    users: [vehicleProcessorEventStoreUser, vehicleReactorEventStoreUser]
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

export const vehicleReactorMongoUser: MongoUser = {
    name: 'vehicle-reactor',
    password: rawMongoConfig.require('vehicleReactorPassword'),
    permissions: [{role: 'readWrite', database: 'dealership'}]
}

export const vehicleReaderMongoUser: MongoUser = {
    name: 'vehicle-reader',
    password: rawMongoConfig.require('vehicleReaderPassword'),
    permissions: [{role: 'read', database: 'dealership'}]
}

export const mongoConfig = {
    adminUser: rawMongoConfig.require('adminUser'),
    adminPassword: rawMongoConfig.require('adminPassword'),
    database: 'dealership',
    users: [vehicleReactorMongoUser, vehicleReaderMongoUser]
}
