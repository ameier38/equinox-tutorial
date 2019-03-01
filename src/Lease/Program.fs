open Suave
open Suave.Operators
open Suave.RequestErrors
open Suave.Filters
open Lease
open Lease.Api
open Lease.Implementation

[<EntryPoint>]
let main argv =
    let config = EventStoreConfig.load()
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
    let resolver = Store.connect config aggregate "lease"
    let service = Service.init aggregate resolver
    let handleGetLease' = handleGetLease service
    let handleCreateLease' = handleCreateLease service
    let handleSchedulePayment' = handleSchedulePayment service
    let handleReceivePayment' = handleReceivePayment service
    let handleModifyLease' = handleModifyLease service
    let handleDeleteLease' = handleDeleteLease service
    let handleUndo' = handleUndo service
    let api = 
        choose
            [ GET >=> choose
                [ pathScan "/lease/%s" (createPathHandler handleGetLease') >=> JSON ]
              POST >=> choose
                [ path "/lease" >=> (createHandler handleCreateLease') >=> JSON
                  pathScan "/lease/%s/schedule" (createPathHandler handleSchedulePayment') >=> JSON
                  pathScan "/lease/%s/payment" (createPathHandler handleReceivePayment') >=> JSON ]
              PUT >=> choose
                [ pathScan "/lease/%s" (createPathHandler handleModifyLease') >=> JSON ]
              DELETE >=> choose
                [ pathScan "/lease/%s" (createPathHandler handleDeleteLease') >=> JSON
                  pathScan "/lease/%s/%s" (createPathHandler handleUndo') >=> JSON ]
              NOT_FOUND "resource not implemented" ]
    let config =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ]}
    startWebServer config api
    0 // return an integer exit code
