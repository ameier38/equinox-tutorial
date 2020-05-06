import * as pulumi from '@pulumi/pulumi'
import * as k8s from '@pulumi/kubernetes'

class Minio extends pulumi.ComponentResource {
    constructor(name: string, opts: pulumi.ComponentResourceOptions) {
        super('infrastructure:Minio', name, {}, opts)

        const minioChart = new k8s.helm.v3.Chart('minio', {
            chart: 'minio',
            fetchOpts: {
                repo: 'https://kubernetes-charts.storage.googleapis.com'
            },
            values: {

            }
            
        })
    }
}