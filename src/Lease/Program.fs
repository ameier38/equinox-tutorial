open Suave
open Suave.Operators
open Suave.Successful
open Suave.Filters
open Lease
open Lease.Api
open Lease.Implementation
open Ouroboros

[<EntryPoint>]
let main argv =
    let config = EventStoreConfig.load()
    let gateway, cache = Store.connect config "lease"
    let aggregate =
        { entity = "lease"
          initial = NonExistent
          isOrigin = Aggregate.isOrigin
          apply = Aggregate.apply
          decide = Aggregate.decide
          reconstitute = Aggregate.reconstitute
          compact = Aggregate.compact
          evolve = Aggregate.evolve
          interpret = Aggregate.interpret }
    let handler = Handler.create aggregate gateway cache
    let handleGetLease' leaseIdParam = 
        handleGetLease aggregate handler leaseIdParam
        |> createHandler
    let handleCreateLease' =
        handleCreateLease handler
        |> createHandler
    let api = 
        choose
            [ GET >=> choose
                [ pathScan "/lease/%s" handleGetLease'
                  path "/" >=> OK "Welcome!" ]
              POST >=> choose
                [ path "/lease" >=> handleCreateLease' ] ]
    let config =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ]}
    startWebServer config api
    0 // return an integer exit code
