import * as pulumi from '@pulumi/pulumi'
import * as digitalocean from '@pulumi/digitalocean'
import * as docker from '@pulumi/docker'
import * as k8s from '@pulumi/kubernetes'
import * as config from './config'
import { dealershipNamespace } from './k8s'

type RegistryArgs = {
    secretNamespace: k8s.core.v1.Namespace
}

export class Registry extends pulumi.ComponentResource {
    dockerCredentials: pulumi.Output<string>
    imageRegistry: pulumi.Output<docker.ImageRegistry>

    constructor(name:string, args:RegistryArgs, opts:pulumi.ComponentResourceOptions) {
        super('infrastructure:Registry', name, {}, opts)

        const registry = new digitalocean.ContainerRegistry(name, {}, { parent: this })
        
        const credentials = new digitalocean.ContainerRegistryDockerCredentials(name, {
            registryName: registry.name
        }, { parent: this })

        this.dockerCredentials = credentials.dockerCredentials

        this.imageRegistry = this.dockerCredentials.apply(creds => {
            const decoded = Buffer.from(creds, 'base64').toString('ascii')
            const parts = decoded.split(':')
            if (parts.length != 2) {
                throw new Error(`Invalid credentials: ${decoded}`)
            }
            return {
                server: registry.serverUrl,
                username: parts[0],
                password: parts[1]
            } as docker.ImageRegistry
        })

        this.registerOutputs({
            dockerCredentials: this.dockerCredentials,
            imageRegistry: this.imageRegistry,
        })
    }
}

export const registry = new Registry('dealership', {
    secretNamespace: dealershipNamespace
}, { providers: [ config.digitalOceanProvider, config.k8sProvider ] })
