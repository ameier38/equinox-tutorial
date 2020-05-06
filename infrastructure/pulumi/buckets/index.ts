import * as digitalocean from '@pulumi/digitalocean'
import * as config from '../config'

const uploadBucket = new digitalocean.SpacesBucket('upload', {
    acl: 'public-read',
    region: digitalocean.Regions.NYC3,
}, {provider: config.digitalOceanProvider})

const exampleObject = new digitalocean.SpacesBucketObject('example', {
    bucket: uploadBucket.name,
    region: digitalocean.Regions.NYC3,
    key: 'hello.txt',
    content: 'world',
    acl: 'public-read'
}, {provider: config.digitalOceanProvider})
