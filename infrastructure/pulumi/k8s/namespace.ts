import * as k8s from '@pulumi/kubernetes'
import * as config from '../config'

export const infrastructureNamespace = new k8s.core.v1.Namespace('infrastructure', {}, { provider: config.k8sProvider })

export const monitoringNamespace = new k8s.core.v1.Namespace('monitoring', {}, { provider: config.k8sProvider })

export const dealershipNamespace = new k8s.core.v1.Namespace('dealership', {}, { provider: config.k8sProvider })
