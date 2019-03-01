open Lease
open Lease.Api
open Lease.Implementation
open Suave

[<EntryPoint>]
let main argv =
    let config = EventStoreConfig.load()
    let aggregate = Aggregate.init()
    let resolver = Store.connect config aggregate "lease"
    let service = Service.init aggregate resolver
    let api' = api service
    let config =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ]}
    startWebServer config api'
    0 // return an integer exit code
