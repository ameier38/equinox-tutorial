import * as pulumi from '@pulumi/pulumi'
import * as digitalocean from '@pulumi/digitalocean'
import * as config from './config'
import * as path from 'path'

const iconPath = path.join(config.root, 'etc', 'images', 'rocket.png')

const uploadBucket = new digitalocean.SpacesBucket('upload', {
    acl: 'public-read'
}, { provider: config.digitalOceanProvider })

const iconObject = new digitalocean.SpacesBucketObject('icon', {
    bucket: uploadBucket.name,
    region: digitalocean.Regions.NYC3,
    key: 'icon.png',
    source: iconPath,
    acl: 'public-read'
}, { provider: config.digitalOceanProvider })

export const iconUrl = pulumi.interpolate `https://${uploadBucket.bucketDomainName}/${iconObject.key}`
