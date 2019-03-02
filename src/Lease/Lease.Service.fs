namespace Lease

open Equinox.EventStore
open Serilog

type Service =
    { get: LeaseId -> ObservationDate -> AsyncResult<string,string>
      create: Lease -> AsyncResult<string,string>
      modify: Lease -> EffectiveDate -> AsyncResult<string,string>
      terminate: LeaseId -> EffectiveDate -> AsyncResult<string,string>
      schedulePayment: LeaseId -> Payment -> AsyncResult<string,string>
      receivePayment: LeaseId -> Payment -> AsyncResult<string,string>
      undo: LeaseId -> EventId -> AsyncResult<string,string> }
module Service =
    let stateProjection 
        (reconstitute: Reconstitute<LeaseEvent,LeaseState>)
        : Projection<LeaseEvent,Result<string,string>> =
        fun (obsDate:ObservationDate) ({ Events = events }) ->
            let leaseState = reconstitute obsDate events
            (leaseState, events)
            |> Dto.LeaseStateSchema.fromDomain
            |> Result.map Dto.LeaseStateSchema.serializeToJson
    let executeCommand
        (aggregate:Aggregate<LeaseCommand,LeaseEvent,LeaseState>)
        (query: Query<LeaseId,LeaseEvent,Result<string,string>>)
        (execute: Execute<LeaseId,LeaseCommand>) =
        fun (leaseId:LeaseId) (command:LeaseCommand) ->
            asyncResult {
                let projection = stateProjection aggregate.reconstitute
                do! execute leaseId command
                let! newState = query leaseId Latest projection
                return! newState |> AsyncResult.ofResult
            }
    let get 
        (aggregate:Aggregate<LeaseCommand,LeaseEvent,LeaseState>)
        (query: Query<LeaseId,LeaseEvent,Result<string,string>>) =
        fun leaseId observationDate ->
            asyncResult {
                let projection = stateProjection aggregate.reconstitute
                let! state = query leaseId observationDate projection
                return! state |> AsyncResult.ofResult
            }
    let create
        (executeCommand:LeaseId -> LeaseCommand -> AsyncResult<string,string>) =
        fun (newLease:Lease) ->
            asyncResult {
                let command =
                    { LeaseId = newLease.LeaseId
                      StartDate = newLease.StartDate
                      MaturityDate = newLease.MaturityDate
                      MonthlyPaymentAmount = newLease.MonthlyPaymentAmount }
                    |> Create
                return! executeCommand newLease.LeaseId command
            }

    let modify 
        (executeCommand:LeaseId -> LeaseCommand -> AsyncResult<string,string>) =
        fun ({ LeaseId = leaseId} as lease) (effDate: EffectiveDate) ->
            asyncResult {
                let command = (lease, effDate) |> Modify
                return! executeCommand leaseId command
            }
    let terminate
        (executeCommand:LeaseId -> LeaseCommand -> AsyncResult<string,string>) =
        fun (leaseId:LeaseId) (effDate:EffectiveDate) ->
            asyncResult {
                let command = effDate |> Terminate    
                return! executeCommand leaseId command
            }
    let schedulePayment 
        (executeCommand:LeaseId -> LeaseCommand -> AsyncResult<string,string>) =
        fun (leaseId:LeaseId) (payment:Payment) ->
            asyncResult {
                let command = payment |> SchedulePayment    
                return! executeCommand leaseId command
            }
    let receivePayment 
        (executeCommand:LeaseId -> LeaseCommand -> AsyncResult<string,string>) =
        fun (leaseId:LeaseId) (payment:Payment) ->
            asyncResult {
                let command = payment |> ReceivePayment    
                return! executeCommand leaseId command
            }
    let undo
        (executeCommand:LeaseId -> LeaseCommand -> AsyncResult<string,string>) =
        fun (leaseId:LeaseId) (eventId:EventId) ->
            asyncResult {
                let command = eventId |> Undo    
                return! executeCommand leaseId command
            }
    let init 
        (aggregate:Aggregate<LeaseCommand,LeaseEvent,LeaseState>) 
        (resolver:GesResolver<LeaseEvent,StreamState<LeaseEvent>>) =
        let log = LoggerConfiguration().WriteTo.Console().CreateLogger()
        let (|AggregateId|) (leaseId: LeaseId) = Equinox.AggregateId(aggregate.entity, LeaseId.toStringN leaseId)
        let (|Stream|) (AggregateId leaseId) = Equinox.Stream(log, resolver.Resolve leaseId, 3)
        let execute (Stream stream) command = stream.Transact(aggregate.interpret command)
        let query : Query<LeaseId,LeaseEvent,'View> =
            fun (Stream stream) (obsDate:ObservationDate) (projection:Projection<LeaseEvent,'View>) -> 
                stream.Query(projection obsDate)
                |> AsyncResult.ofAsync
        let executeCommand' = executeCommand aggregate query execute
        { get = get aggregate query
          create = create executeCommand'
          modify = modify executeCommand'
          terminate = terminate executeCommand'
          schedulePayment = schedulePayment executeCommand'
          receivePayment = receivePayment executeCommand'
          undo = undo executeCommand' }
