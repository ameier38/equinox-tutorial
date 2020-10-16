import * as pulumi from '@pulumi/pulumi'
import * as digitalocean from '@pulumi/digitalocean'
import * as k8s from '@pulumi/kubernetes'
import * as docker from '@pulumi/docker'
import * as path from 'path'
import * as config from './config'
import { Registry, dealershipRegistry } from './registry'

type WebAppArgs = {
    namespace: k8s.core.v1.Namespace
    registry: Registry
}

class WebApp extends pulumi.ComponentResource {
    constructor(name:string, args:WebAppArgs, opts:pulumi.ComponentResourceOptions) {
        super('infrastructure:WebApp', name, {}, opts)

        const webAppImage = new docker.Image(name, {
            imageName: pulumi.interpolate `${args.registry.imageRegistry.server}/${name}`,
            build: {
                context: path.join(config.root, 'fable-web-app'),
                dockerfile: path.join(config.root, 'fable-web-app', 'deploy', 'Dockerfile'),
                args: { 
                    RUNTIME_IMAGE: 'nginx:1.17-alpine',
                    APP_SCHEME: 'https',
                    APP_HOST: config.dnsConfig.tld,
                    APP_PORT: '8080',
                    GRAPHQL_API_SCHEME: 'https',
                    GRAPHQL_API_HOST: `graphql.${config.dnsConfig.tld}`,
                    GRAPHQL_API_PORT: '80'
                }
            },
            registry: { 
                server: args.registry.serverUrl,
                username: args.registry.username,
                password: args.registry.password
            }
        }, { parent: this })

        const webAppChart = new k8s.helm.v3.Chart('web-app', {
            path: path.join(config.root, 'infrastructure', 'charts', 'base-service'),
            namespace: namespace,
            values: {
                fullnameOverride: 'web-app',
                isGrpc: false,
                image: webAppImage.imageName
            }
        }, { parent: this })

        const serviceHost =
            pulumi.all([webAppChart, namespace])
            .apply(([chart, ns]) => chart.getResourceProperty('v1/Service', ns, 'web-app', 'metadata'))
            .apply(meta => `${meta.name}.${meta.namespace}.svc.cluster.local`)

        const servicePort =
            pulumi.all([webAppChart, namespace])
            .apply(([chart, ns]) => chart.getResourceProperty('v1/Service', ns, 'web-app', 'spec'))
            .apply(spec => spec.ports.find(port => port.name === 'http')?.port)
    }
}