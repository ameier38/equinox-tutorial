import * as auth0 from '@pulumi/auth0'
import * as pulumi from '@pulumi/pulumi'
import * as config from './config'

type Scope = {
    value: string
    description: string
}

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
            callback(null, user, context);
        }).catch(function (err) {
            callback(err);
        });
}`
} 

type IdentityProviderArgs = {
    audience: pulumi.Input<string>
    customerScopes: Scope[]
    adminScopes: Scope[]
    clients: auth0.Client[]
}

export class IdentityProvider extends pulumi.ComponentResource {
    constructor(name:string, args:IdentityProviderArgs, opts:pulumi.ComponentResourceOptions) {
        super('cosmicdealership:IdentityProvider', name, {}, opts)

        const databaseConnection = new auth0.Connection(name, {
            name: 'database',
            displayName: 'Database',
            strategy: 'auth0',
            enabledClients: args.clients.map(client => client.id)
        }, { parent: this })

        const setRolesRule = new auth0.Rule(`${name}-set-roles`, {
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
        
        const customerRole = new auth0.Role('cosmic-dealership-customer', {
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
    }
}

let listVehiclesScope = { value: 'list:vehicles', description: 'Can list vehicles' }
let getVehiclesScope = { value: 'get:vehicles', description: 'Can get a vehicle' }
let addVehiclesScope = { value: 'add:vehicles', description: 'Can create a vehicle' }
let removeVehiclesScope = { value: 'remove:vehicles', description: 'Can delete a vehicle' }
let leaseVehiclesScope = { value: 'lease:vehicle', description: 'Can lease a vehicle' }
let returnVehiclesScope = { value: 'return:vehicles', description: 'Can return a leased vehicle' }

export const identityProvider = new IdentityProvider('dealership', {
    audience: config.audience,
    clients: [],
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
