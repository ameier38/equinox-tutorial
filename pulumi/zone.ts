import * as cloudflare from '@pulumi/cloudflare'
import * as config from './config'

export const zone = new cloudflare.Zone(config.zone, {
    zone: config.zone
}, { provider: config.cloudflareProvider })
