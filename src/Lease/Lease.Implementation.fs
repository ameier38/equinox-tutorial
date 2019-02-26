module Lease.Implementation

open Ouroboros
open Equinox.EventStore
open FSharp.UMX
open Serilog
open System

module LeaseEvent =
    let (|Order|) { CreatedDate = createdDate; EffectiveDate = effDate } = (effDate, createdDate)
    let getContext = function
        | Undid _ -> None
        | Created (_, ctx) -> ctx |> Some
        | Modified (_, ctx) -> ctx |> Some
        | PaymentScheduled (_, ctx) -> ctx |> Some
        | PaymentReceived (_, ctx) -> ctx |> Some
        | Terminated ctx -> ctx |> Some
    let getOrder = getContext >> Option.map (fun (Order order) -> order)

module Aggregate =
    let applyError event state = sprintf "%A cannot be applied to state %A" event state |> Corrupt

    let commandError command state = sprintf "cannot execte %A in state %A" command state |> Error

    let (|Order|) leaseEvent = LeaseEvent.getOrder leaseEvent

    let apply : Apply<LeaseState,LeaseEvent> =
        fun state -> function
            | Undid _ -> "Undid event should be filtered from lease events" |> Corrupt
            | Created (lease, _) ->
                match state with
                | NonExistent ->
                    { Lease = lease
                      TotalScheduled = 0m
                      TotalPaid = 0m
                      AmountDue = 0m }
                    |> Outstanding
                | _ -> applyError Created state
            | Modified (lease, _) ->
                match state with
                | Outstanding data ->
                    { data with
                        Lease = lease } 
                    |> Outstanding
                | _ -> applyError Modified state
            | PaymentScheduled (amount, _) ->
                match state with
                | Outstanding data ->
                    let newTotalScheduled = data.TotalScheduled + amount
                    { data with
                        TotalScheduled = newTotalScheduled
                        AmountDue = newTotalScheduled - data.TotalPaid }
                    |> Outstanding
                | _ -> applyError PaymentScheduled state
            | PaymentReceived (amount, _) ->
                match state with
                | Outstanding data ->
                    let newTotalPaid = data.TotalPaid + amount
                    { data with
                        TotalPaid = newTotalPaid
                        AmountDue = data.TotalScheduled - newTotalPaid }
                    |> Outstanding
                | _ -> applyError PaymentScheduled state
            | Terminated _ ->
                match state with    
                | Outstanding data ->
                    LeaseState.Terminated data
                | _ -> applyError PaymentScheduled state

    let decide : Decide<LeaseCommand,LeaseState,LeaseEvent> =
        fun command state ->
            match command with
            | Undo _ -> Ok []
            | Create ({ StartDate = startDate } as lease) ->
                match state with
                | NonExistent -> 
                    let meta = EventMeta.create nextId (% startDate)
                    Created (meta, lease) |> List.singleton |> Ok
                | _ -> commandError Create state
            | Modify (effDate, lease) ->
                match state with
                | Outstanding _ -> 
                    let meta = EventMeta.create nextId effDate
                    Modified (meta, lease) |> List.singleton |> Ok
                | _ -> commandError Modify state
            | SchedulePayment ({ PaymentDate = pmtDate } as payment) ->
                match state with
                | Outstanding _ -> 
                    let meta = EventMeta.create nextId (% pmtDate)
                    PaymentScheduled (meta, payment) |> List.singleton |> Ok
                | _ -> commandError SchedulePayment state
            | ReceivePayment ({ PaymentDate = pmtDate } as payment) ->
                match state with
                | Outstanding _ -> 
                    let meta = EventMeta.create nextId (% pmtDate)
                    PaymentReceived (meta, payment) |> List.singleton |> Ok
                | _ -> commandError ReceivePayment state
            | Terminate effDate ->
                match state with
                | Outstanding _ -> 
                    let meta = EventMeta.create nextId effDate
                    Terminated meta |> List.singleton |> Ok
                | _ -> commandError Terminate state

    let evolve : Evolve<LeaseEvent> =
        fun state event ->
            match event with
            | Undid undoEventId -> { state with Items = state.Items |> List.filter (fun { EventId = eventId } -> eventId <> undoEventId ) }
            | _ -> event :: state

    let onOrBeforeObservationDate 
        observationDate 
        { CreatedDate = createdDate; EffectiveDate = effectiveDate } =
        match observationDate with
        | Latest -> true
        | AsOf asOfDate ->
            createdDate <= % asOfDate
        | AsAt asAtDate ->
            effectiveDate <= % asAtDate

    let reconstitute : Reconstitute<LeaseEvent,LeaseState> =
        fun observationDate events ->
            events
            |> List.filter (onOrBeforeObservationDate observationDate)
            |> List.sortBy (fun { CreatedDate = created; EffectiveDate = effective } -> (effective, created))
            |> List.map (fun { DomainEvent = domainEvent } -> domainEvent)
            |> List.fold apply NonExistent

    let interpret : Interpret<LeaseCommand,LeaseEvent> =
        let ok = Ok ()
        fun { EffectiveDate = effectiveDate; DomainCommand = command } events ->
            let asAt = % effectiveDate |> AsAt
            let reconstitute' = reconstitute asAt
            match command with
            | Undo undoEventId ->
                if events |> List.exists (fun { EventId = eventId} -> eventId = undoEventId)
                then 
                    let newEvents =
                        (effectiveDate, Undid undoEventId) 
                        ||> Event.createSingleton
                    (ok, newEvents)
                else
                    let error = sprintf "Event with id %A does not exist" undoEventId |> Error
                    (error, [])
            | Create lease ->
                match reconstitute' events with
                | NonExistent -> 
                    let newEvents =
                        (effectiveDate, Created lease) 
                        ||> Event.createSingleton
                    (ok, newEvents)
                | _ as state ->
                    let error = sprintf "cannot create lease in state %A" state |> Error
                    (error, [])
            | Modify lease ->
                match reconstitute' events with
                | Outstanding _ ->
                    let newEvents =
                        (effectiveDate, Modified lease)
                        ||> Event.createSingleton
                    (ok, newEvents)
                | _ as state ->
                    let error = sprintf "cannot modify lease in state %A" state |> Error
                    (error, [])
            | SchedulePayment amount ->
                match reconstitute' events with
                | Outstanding _ ->
                    let newEvents =
                        (effectiveDate, PaymentScheduled amount)
                        ||> Event.createSingleton
                    (ok, newEvents)
                | _ as state ->
                    let error = sprintf "cannot schedule payment in state %A" state |> Error
                    (error, [])
            | ReceivePayment amount ->
                match reconstitute' events with
                | Outstanding _ ->
                    let newEvents =
                        (effectiveDate, PaymentReceived amount)
                        ||> Event.createSingleton
                    (ok, newEvents)
                | _ as state ->
                    let error = sprintf "cannot receive payment in state %A" state |> Error
                    (error, [])
            | Terminate ->
                match reconstitute' events with
                | Outstanding _ ->
                    let newEvents =
                        (effectiveDate, Terminated)
                        ||> Event.createSingleton
                    (ok, newEvents)
                | _ as state ->
                    let error = sprintf "cannot receive payment in state %A" state |> Error
                    (error, [])

module Store =
    let connect (config:EventStoreConfig) (name:string) =
        let uri = 
            sprintf "%s://@%s:%d" 
                config.Protocol 
                config.Host 
                config.Port
            |> Uri
        let timeout = TimeSpan.FromSeconds 5.0
        let log = Log.Logger |> Logger.SerilogNormal
        let connector = 
            GesConnector(
                config.User, 
                config.Password, 
                reqTimeout=timeout, 
                reqRetries=1, 
                log=log)
        let cache = Caching.Cache ("ES", 20)
        let strategy = ConnectionStrategy.ClusterTwinPreferSlaveReads
        let conn = 
            connector.Establish(name, Discovery.Uri uri, strategy)
            |> Async.RunSynchronously
        let gateway = GesGateway(conn, GesBatchingPolicy(maxBatchSize=500))
        (gateway, cache)

module Handler =
    let log = LoggerConfiguration().WriteTo.Console().CreateLogger()
    let (|AggregateId|) (leaseId: LeaseId) = Equinox.AggregateId("lease", LeaseId.toStringN leaseId)
    let (|Stream|) (AggregateId dogId) = Equinox.Stream(log, resolveStream dogId, defaultArg maxAttempts 3)
    let create 
        (aggregate:Aggregate<LeaseState,LeaseCommand,LeaseEvent>) 
        (gateway:GesGateway) 
        (cache:Caching.Cache) =
        let accessStrategy = Equinox.EventStore.AccessStrategy.RollingSnapshots (aggregate.isOrigin, aggregate.compact)
        let cacheStrategy = Equinox.EventStore.CachingStrategy.SlidingWindow (cache, TimeSpan.FromMinutes 20.)
        let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
        let codec = Equinox.UnionCodec.JsonUtf8.Create<LeaseEvent>(serializationSettings)
        let resolver = GesResolver(gateway, codec, aggregate.evolve, aggregate.initial, accessStrategy, cacheStrategy)
        let inner = Equinox.Handler(Aggregate.fold, log, stream, maxAttempts = 2)
        let execute command = 
            try
                inner.Decide(fun ctx -> 
                    ctx.Execute (interpret command)
                    ctx.State)
                |> AsyncResult.ofAsync
            with 
            | exn ->
                sprintf "execute failed: \n%A" exn
                |> DogError
                |> AsyncResult.ofError
        let query () = 
            try
                inner.Query(id)
                |> AsyncResult.ofAsync
            with
            | exn ->
                sprintf "query failed: \n%A" exn
                |> DogError
                |> AsyncResult.ofError
        { execute = execute
          query = query }
