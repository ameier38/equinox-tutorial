import * as auth0 from '@pulumi/auth0'
import * as cloudflare from '@pulumi/cloudflare'
import * as k8s from '@pulumi/kubernetes'
import * as pulumi from '@pulumi/pulumi'
import { Gateway } from './gateway'

type ClientArgs = {
    namespace: k8s.core.v1.Namespace
    gateway: Gateway
    zone: cloudflare.Zone
    subdomain: pulumi.Input<string>
    authUrl: pulumi.Input<string>
    serviceHost: pulumi.Input<string>
    servicePort: pulumi.Input<number>
}

export class Client extends pulumi.ComponentResource {
    host: pulumi.Output<string>

    constructor(name:string, args:ClientArgs, opts:pulumi.ComponentResourceOptions) {
        super('infrastructure:Client', name, {}, opts)
        const record = new cloudflare.Record(name, {
            zoneId: args.zone.id,
            name: args.subdomain,
            type: 'CNAME',
            value: args.gateway.loadBalancerHost
        }, { parent: this })

        this.host = record.hostname

        const authClient = new auth0.Client(name, {
            name: name,
            description: `${name} client`,
            appType: 'non_interactive',
            tokenEndpointAuthMethod: 'client_secret_post',
            callbacks: [
                pulumi.interpolate `https://${record.hostname}/.ambassador/oauth2/redirection-endpoint`,
            ],
        }, { parent: this })

        const authFilter = new k8s.apiextensions.CustomResource(name, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Filter',
            metadata: { namespace: args.namespace.metadata.name },
            spec: {
                OAuth2: {
                    authorizationURL: args.authUrl,
                    extraAuthorizationParameters: {
                        audience: pulumi.interpolate `${args.authUrl}/api/v2/`
                    },
                    clientID: authClient.clientId,
                    secret: authClient.clientSecret,
                    protectedOrigins: [{
                        origin: record.hostname
                    }]
                }
            }
        }, { parent: this, dependsOn: args.gateway })

        new k8s.apiextensions.CustomResource(name, {
            apiVersion: 'getambassador.io/v2',
            kind: 'FilterPolicy',
            metadata: { namespace: args.namespace.metadata.name },
            spec: {
                rules: [{
                    host: record.hostname,
                    path: '*',
                    filters: [{
                        name: authFilter.metadata.name,
                        arguments: {
                            scopes: ['openid']
                        }
                    }]
                }]
            }
        }, { parent: this, dependsOn: args.gateway })

        new k8s.apiextensions.CustomResource(name, {
            apiVersion: 'getambassador.io/v2',
            kind: 'Mapping',
            metadata: { namespace: args.namespace.metadata.name },
            spec: {
                prefix: '/',
                host: record.hostname,
                service: pulumi.interpolate `${args.serviceHost}:${args.servicePort}`
            }
        }, { parent: this, dependsOn: args.gateway })

        this.registerOutputs({
            host: this.host
        })
    }
}
