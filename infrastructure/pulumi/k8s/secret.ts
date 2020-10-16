import * as k8s from '@pulumi/kubernetes'
import { dealershipNamespace } from './namespace'
import { registry } from '../registry'
import * as config from '../config'

export const registrySecret = new k8s.core.v1.Secret('registry-secret', {
    metadata: { namespace: dealershipNamespace.metadata.name },
    type: 'kubernetes.io/dockerconfigjson',
    stringData: {
        '.dockerconfigjson': registry.dockerCredentials
    }
}, { provider: config.k8sProvider })

export const eventstoreSecret = new k8s.core.v1.Secret('eventstore', {
    metadata: { namespace: dealershipNamespace.metadata.name },
    stringData: {
        user: config.eventstoreConfig.user,
        password: config.eventstoreConfig.password
    }
}, { provider: config.k8sProvider })

export const mongoSecret = new k8s.core.v1.Secret('mongo', {
    metadata: { namespace: dealershipNamespace.metadata.name },
    stringData: {
        user: config.mongoConfig.user,
        password: config.mongoConfig.password
    }
}, { provider: config.k8sProvider })

export const auth0Secret = new k8s.core.v1.Secret('auth0', {
    metadata: { namespace: dealershipNamespace.metadata.name },
    stringData: {
        'client-id': config.auth0Config.clientId,
        'client-secret': config.auth0Config.clientSecret
    }
}, { provider: config.k8sProvider })
