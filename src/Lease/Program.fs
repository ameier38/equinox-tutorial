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
    let handleGetLease' leaseIdParam = 
        handleGetLease service leaseIdParam
        |> createHandler
    let handleCreateLease' =
        handleCreateLease service
        |> createHandler
    let handleModifyLease' leaseIdParam = 
        handleModifyLease service leaseIdParam
        |> createHandler
    let handleDeleteLease' leaseIdParam =
        handleDeleteLease service leaseIdParam
        |> createHandler
    let handleSchedulePayment' leaseIdParam =
        handleSchedulePayment service leaseIdParam
        |> createHandler
    let handleReceivePayment' leaseIdParam =
        handleReceivePayment service leaseIdParam
        |> createHandler
    let handleUndo' (leaseIdParam, eventIdParam) =
        handleUndo service leaseIdParam eventIdParam
        |> createHandler
    let api = 
        choose
            [ GET >=> choose
                [ pathScan "/lease/%s" handleGetLease' ]
              POST >=> choose
                [ path "/lease" >=> handleCreateLease'
                  pathScan "/lease/%s/schedule" handleSchedulePayment'
                  pathScan "/lease/%s/payment" handleReceivePayment' ]
              PUT >=> choose
                [ pathScan "/lease/%s" handleModifyLease' ]
              DELETE >=> choose
                [ pathScan "/lease/%s" handleDeleteLease'
                  pathScan "/lease/%s/%s" handleUndo' ]
              NOT_FOUND "resource not implemented" ]
    let config =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ]}
    startWebServer config api
    0 // return an integer exit code
