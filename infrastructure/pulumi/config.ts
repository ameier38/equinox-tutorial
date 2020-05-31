import * as pulumi from '@pulumi/pulumi'
import * as auth0 from '@pulumi/auth0'
import * as digitalocean from '@pulumi/digitalocean'
import * as k8s from '@pulumi/kubernetes'
import * as path from 'path'

export const root = path.dirname(path.dirname(__dirname))

export const env = pulumi.getStack()

const rawDnsConfig = new pulumi.Config('dns')
export const dnsConfig = {
    tld: rawDnsConfig.require('tld')
}

const rawDigitalOceanConfig = new pulumi.Config('digitalocean')
export const digitalOceanProvider = new digitalocean.Provider(`${env}-do-provider`, {
    apiEndpoint: 'api.digitalocean.com/v2',
    token: rawDigitalOceanConfig.require('token'),
    spacesEndpoint: `${digitalocean.Regions.NYC3}.digitaloceanspaces.com`,
    spacesAccessId: rawDigitalOceanConfig.require('spacesAccessId'),
    spacesSecretKey: rawDigitalOceanConfig.require('spacesSecretKey')
})

const rawK8sConfig = new pulumi.Config('k8s')
export const k8sProvider = new k8s.Provider(`${env}-k8s-provider`, {
    kubeconfig: rawK8sConfig.require('kubeconfig')
})

const rawAuth0Config = new pulumi.Config('auth0')
export const auth0Provider = new auth0.Provider(`${env}-auth0-provider`, {
    domain: rawAuth0Config.require('domain'),
    clientId: rawAuth0Config.require('clientId'),
    clientSecret: rawAuth0Config.require('clientSecret')
})
