open Lease
open Lease.Aggregate
open Lease.Store
open Lease.Service
open Suave
open Serilog

[<EntryPoint>]
let main argv =
    let logger = LoggerConfiguration().WriteTo.Console().CreateLogger()
    let config = EventStoreConfig.load()
    let aggregate = Aggregate()
    let store = Store(config, aggregate)
    let service = Service(aggregate, store, logger)
    let api = Api.init service
    let config =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ]}
    startWebServer config api
    0 // return an integer exit code
