import * as pulumi from '@pulumi/pulumi'
import * as digitalocean from '@pulumi/digitalocean'
import * as k8s from '@pulumi/kubernetes'

export const env = pulumi.getStack()

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
