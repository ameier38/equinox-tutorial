import * as k8s from '@pulumi/kubernetes'
import * as config from './config'

export const cosmicdealershipNamespace = new k8s.core.v1.Namespace('cosmicdealership', {
    metadata: { name: 'cosmicdealership' }
}, { provider: config.k8sProvider })
