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
    oauthAudience: pulumi.Input<string>
    clientId: pulumi.Input<string>
    customerScopes: Scope[]
    adminScopes: Scope[]
    testAdminEmail: pulumi.Input<string>
    testAdminPassword: pulumi.Input<string>
    testCustomerEmail: pulumi.Input<string>
    testCustomerPassword: pulumi.Input<string>
    iconUrl: pulumi.Input<string>
}

export class IdentityProvider extends pulumi.ComponentResource {
    webAppClientId: pulumi.Output<string>
    webAppClientSecret: pulumi.Output<string>

    constructor(name:string, args:IdentityProviderArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:IdentityProvider', name, {}, opts)

        // NB: used by web app to get token for user
        const webAppClient = new auth0.Client(`${name}-web-app`, {
            name: args.zone,
            logoUri: args.iconUrl,
            appType: 'spa',
            tokenEndpointAuthMethod: 'none',
            callbacks: [
                'https://web-app.proxy',
                pulumi.interpolate `https://${args.zone}`,
            ],
            allowedLogoutUrls: [
                'https://web-app.proxy',
                pulumi.interpolate `https://${args.zone}`,
            ],
            webOrigins: [
                'https://web-app.proxy',
                pulumi.interpolate `https://${args.zone}`
            ],
            allowedOrigins: [
                'https://web-app.proxy',
                pulumi.interpolate `https://${args.zone}`
            ],
            grantTypes: ['authorization_code'],
            jwtConfiguration: {
                // NB: Auth0 JavaScript library only uses RS256
                alg: 'RS256'
            }
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
            ],
            options: {
                disableSignup: true,
                passwordPolicy: 'low',
                passwordComplexityOptions: {
                    minLength: 8
                }
            },
        }, { parent: this, deleteBeforeReplace: true })

        new auth0.ClientGrant(`${name}-web-app`, {
            clientId: webAppClient.clientId,
            audience: args.oauthAudience,
            scopes: ['openid', 'email', 'profile'],
        }, { parent: this, deleteBeforeReplace: true })

        new auth0.Rule(`${name}-set-roles`, {
            name: 'Set Roles',
            enabled: true,
            script: pulumi.output(args.oauthAudience).apply(generateSetRolesRuleScript),
            order: 0
        }, { parent: this })

        // NB: used to specify exposed scopes
        // ref: https://auth0.com/docs/authorization/apis
        const resourceServer = new auth0.ResourceServer(name, {
            name: args.zone,
            identifier: args.oauthAudience,
            // NB: turns on RBAC
            enforcePolicies: true,
            // NB: includes 'permissions' claim
            tokenDialect: 'access_token_authz',
            scopes: [...args.customerScopes, ...args.adminScopes]
        }, { parent: this })
        
        const customerRole = new auth0.Role('customer', {
            name: 'customer',
            description: 'A customer of Cosmic Dealership',
            permissions: args.customerScopes.map(scope => ({
                name: scope.value,
                resourceServerIdentifier: resourceServer.identifier
            } as auth0.types.input.RolePermission))
        }, { parent: this, deleteBeforeReplace: true })

        const adminRole = new auth0.Role('admin', {
            name: 'admin',
            description: 'An administrator of Cosmic Dealership',
            permissions: args.adminScopes.map(scope => ({
                name: scope.value,
                resourceServerIdentifier: resourceServer.identifier
            } as auth0.types.input.RolePermission))
        }, { parent: this, deleteBeforeReplace: true })

        new auth0.User('test-admin', {
            name: 'test-admin',
            email: args.testAdminEmail,
            emailVerified: true,
            connectionName: connection.name,
            password: args.testAdminPassword,
            roles: [adminRole.id]
        }, { parent: this, deleteBeforeReplace: true })

        new auth0.User('test-customer', {
            name: 'test-customer',
            email: args.testCustomerEmail,
            emailVerified: true,
            connectionName: connection.name,
            password: args.testCustomerPassword,
            roles: [customerRole.id]
        }, { parent: this })

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
    oauthAudience: config.oauthAudience,
    clientId: config.auth0Config.clientId,
    iconUrl: iconUrl,
    testAdminEmail: config.auth0Config.testAdminEmail,
    testAdminPassword: config.auth0Config.testAdminPassword,
    testCustomerEmail: config.auth0Config.testCustomerEmail,
    testCustomerPassword: config.auth0Config.testCustomerPassword,
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
