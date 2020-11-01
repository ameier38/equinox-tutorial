import * as auth0 from '@pulumi/auth0'
import * as pulumi from '@pulumi/pulumi'
import * as config from './config'
import { iconUrl } from './bucket'

type Scope = {
    value: string
    description: string
}

// NB: add roles to id token so we can display different content on the web app
// ref: https://auth0.com/docs/authorization/sample-use-cases-rules-with-authorization#add-user-roles-to-tokens
function generateSetRolesRuleScript(audience:string) {
    return `
function setRolesToUser(user, context, callback) {
    const assignedRoles = (context.authorization || {}).roles;
    // Roles should only be set to verified users.
    if (!user.email || !user.email_verified) {
        return callback(null, user, context);
    }

    user.app_metadata = user.app_metadata || {};

    user.app_metadata.roles = assignedRoles;
    auth0.users.updateAppMetadata(user.user_id, user.app_metadata)
        .then(function () {
            context.idToken[\`${audience}/roles\`] = user.app_metadata.roles;
            return callback(null, user, context);
        }).catch(function (err) {
            return callback(err);
        });
}`
} 

type IdentityProviderArgs = {
    zone: pulumi.Input<string>
    audience: pulumi.Input<string>
    clientId: pulumi.Input<string>
    customerScopes: Scope[]
    adminScopes: Scope[]
    iconUrl: pulumi.Input<string>
}

export class IdentityProvider extends pulumi.ComponentResource {
    webAppClientId: pulumi.Output<string>
    webAppClientSecret: pulumi.Output<string>

    constructor(name:string, args:IdentityProviderArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:IdentityProvider', name, {}, opts)

        // NB: used by web app to get token for user
        const webAppClient = new auth0.Client(`${name}-web-app`, {
            name: `${name}-web-app`,
            logoUri: args.iconUrl,
            appType: 'spa',
            callbacks: [
                'http://localhost:3000',
                pulumi.interpolate `https://${args.zone}`,
            ],
            allowedLogoutUrls: [
                'http://localhost:3000',
                pulumi.interpolate `https://${args.zone}`,
            ],
        }, { provider: config.auth0Provider })

        this.webAppClientId = webAppClient.clientId
        this.webAppClientSecret = webAppClient.clientSecret

        const connection = new auth0.Connection(name, {
            name: 'database',
            displayName: 'Database',
            strategy: 'auth0',
            enabledClients: [
                // NB: pulumi client id, required to create users
                args.clientId,
                webAppClient.clientId
            ]
        }, { parent: this })

        new auth0.ClientGrant(`${name}-gateway`, {
            clientId: webAppClient.clientId,
            audience: args.audience,
            scopes: ['openid', 'email', 'profile'],
        }, { parent: this })

        new auth0.Rule(`${name}-set-roles`, {
            name: 'Set Roles',
            enabled: true,
            script: pulumi.output(args.audience).apply(generateSetRolesRuleScript),
            order: 0
        }, { parent: this })

        // NB: used to specify exposed scopes
        // ref: https://auth0.com/docs/authorization/apis
        const resourceServer = new auth0.ResourceServer(name, {
            identifier: args.audience,
            // NB: turns on RBAC
            enforcePolicies: true,
            // NB: includes 'permissions' claim
            tokenDialect: 'access_token_authz',
            scopes: [...args.customerScopes, ...args.adminScopes]
        }, { parent: this })
        
        new auth0.Role('cosmic-dealership-customer', {
            name: 'Cosmic Dealership Customer',
            description: 'A customer of Cosmic Dealership',
            permissions: args.customerScopes.map(scope => ({
                name: scope.value,
                resourceServerIdentifier: resourceServer.identifier
            } as auth0.types.input.RolePermission))
        }, { parent: this })

        const adminRole = new auth0.Role('cosmic-dealership-admin', {
            name: 'Cosmic Dealership Admin',
            description: 'An administrator of Cosmic Dealership',
            permissions: args.adminScopes.map(scope => ({
                name: scope.value,
                resourceServerIdentifier: resourceServer.identifier
            } as auth0.types.input.RolePermission))
        }, { parent: this })

        new auth0.User('admin', {
            name: 'admin',
            email: config.auth0Config.adminEmail,
            emailVerified: true,
            connectionName: connection.name,
            password: config.auth0Config.adminPassword,
            roles: [adminRole.id]
        }, { provider: config.auth0Provider })

        this.registerOutputs({
            webAppClientId: this.webAppClientId,
            webAppClientSecret: this.webAppClientSecret
        })
    }
}

let listVehiclesScope = { value: 'list:vehicles', description: 'Can list vehicles' }
let getVehiclesScope = { value: 'get:vehicles', description: 'Can get a vehicle' }
let addVehiclesScope = { value: 'add:vehicles', description: 'Can create a vehicle' }
let removeVehiclesScope = { value: 'remove:vehicles', description: 'Can delete a vehicle' }
let leaseVehiclesScope = { value: 'lease:vehicle', description: 'Can lease a vehicle' }
let returnVehiclesScope = { value: 'return:vehicles', description: 'Can return a leased vehicle' }

export const identityProvider = new IdentityProvider(config.env, {
    zone: config.zone,
    audience: config.audience,
    clientId: config.auth0Config.clientId,
    iconUrl: iconUrl,
    adminScopes: [
        listVehiclesScope,
        getVehiclesScope,
        addVehiclesScope,
        removeVehiclesScope,
        leaseVehiclesScope,
        returnVehiclesScope
    ],
    customerScopes: [
        listVehiclesScope,
        getVehiclesScope,
        leaseVehiclesScope,
        returnVehiclesScope
    ]
}, { provider: config.auth0Provider })
