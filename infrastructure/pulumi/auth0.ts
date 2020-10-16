import * as pulumi from '@pulumi/pulumi'
import * as auth0 from '@pulumi/auth0'
import * as config from './config'
import { iconUrl } from './bucket'

let listVehiclesScope = { value: 'list:vehicles', description: 'Can list vehicles' }
let getVehiclesScope = { value: 'get:vehicles', description: 'Can get a vehicle' }
let addVehiclesScope = { value: 'add:vehicles', description: 'Can create a vehicle' }
let removeVehiclesScope = { value: 'remove:vehicles', description: 'Can delete a vehicle' }
let leaseVehiclesScope = { value: 'lease:vehicle', description: 'Can lease a vehicle' }
let returnVehiclesScope = { value: 'return:vehicles', description: 'Can return a leased vehicle' }
let listLeasesScope = { value: 'list:leases', description: 'Can list leases' }
let getLeasesScope = { value: 'get:leases', description: 'Can get a lease' }
let createLeasesScope = { value: 'create:leases', description: 'Can create a lease' }
let terminateLeasesScope = { value: 'terminate:leases', description: 'Can terminate a lease' }

const webClient = new auth0.Client(`${name}-web`, {
    name: `${name}-web`,
    description: 'Web client',
    logoUri: iconUrl,
    appType: 'spa',
    callbacks: [
        'http://localhost:3000',
        `https://${config.dnsConfig.zone}`,
    ],
    allowedLogoutUrls: [
        'http://localhost:3000',
        `https://${config.dnsConfig.zone}`,
    ],
}, { provider: config.auth0Provider })

const databaseConnection = new auth0.Connection('database', {
    name: 'database',
    displayName: 'Database',
    strategy: 'auth0',
    enabledClients: [
        webClient.id,
    ]
}, { provider: config.auth0Provider })

const setRolesRuleScript = `
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
    context.idToken[\`${config.dnsConfig.zone}/roles\`] = user.app_metadata.roles;
    callback(null, user, context);
    })
    .catch(function (err) {
    callback(err);
    });
}`

const setRolesRule = new auth0.Rule('set-roles', {
    name: 'Set Roles',
    enabled: true,
    script: setRolesRuleScript,
    order: 0
}, { provider: config.auth0Provider })

const dealershipResourceServer = new auth0.ResourceServer('dealership', {
    name: 'Cosmic Dealership',
    identifier: `https://${config.dnsConfig.zone}`,
    // turns on RBAC
    enforcePolicies: true,
    // includes 'permissions' claim
    tokenDialect: 'access_token_authz',
    scopes: [
        listVehiclesScope,
        getVehiclesScope,
        addVehiclesScope,
        removeVehiclesScope,
        leaseVehiclesScope,
        returnVehiclesScope,
        listLeasesScope,
        getLeasesScope,
        createLeasesScope,
        terminateLeasesScope
    ]
}, { provider: config.auth0Provider })

const customerRole = new auth0.Role('cosmic-dealership-customer', {
    name: 'Cosmic Dealership Customer',
    description: 'A customer of Cosmic Dealership',
    permissions: [
        listVehiclesScope,
        getVehiclesScope,
        leaseVehiclesScope,
        returnVehiclesScope,
        listLeasesScope,
        getLeasesScope
    ].map(scope => ({
        name: scope.value,
        resourceServerIdentifier: dealershipResourceServer.identifier
    } as auth0.types.input.RolePermission))
}, { provider: config.auth0Provider })

const adminRole = new auth0.Role('cosmic-dealership-admin', {
    name: 'Cosmic Dealership Admin',
    description: 'An administrator of Cosmic Dealership',
    permissions: [
        listVehiclesScope,
        getVehiclesScope,
        addVehiclesScope,
        removeVehiclesScope,
        leaseVehiclesScope,
        returnVehiclesScope,
        listLeasesScope,
        getLeasesScope,
        createLeasesScope,
        terminateLeasesScope
    ].map(scope => ({
        name: scope.value,
        resourceServerIdentifier: dealershipResourceServer.identifier
    } as auth0.types.input.RolePermission))
}, { provider: config.auth0Provider })
