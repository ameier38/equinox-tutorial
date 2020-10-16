import * as cloudflare from '@pulumi/cloudflare'
import * as config from './config'

export const zone = new cloudflare.Zone('cosmicdealership', {
    zone: config.dnsConfig.zone
}, { provider: config.cloudflareProvider })
