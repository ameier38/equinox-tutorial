module Lease.Implementation

open Equinox.EventStore
open FSharp.UMX
open Ouroboros
open Serilog
open System

module LeaseEvent =
    let (|Order|) { CreatedDate = createdDate; EffectiveDate = effDate } = (effDate, createdDate)
    let getContext = function
        | Undid _ -> None
        | Compacted _ -> None
        | Created (_, ctx) -> ctx |> Some
        | Modified (_, ctx) -> ctx |> Some
        | PaymentScheduled (_, ctx) -> ctx |> Some
        | PaymentReceived (_, ctx) -> ctx |> Some
        | LeaseEvent.Terminated ctx -> ctx |> Some
    let getOrder = getContext >> Option.map (fun (Order order) -> order)
    let getEventId = getContext >> Option.map (fun { EventId = eventId } -> eventId)

module Aggregate =
    let applyError event state = sprintf "%A cannot be applied to state %A" event state |> Corrupt

    let commandError command state = sprintf "cannot execte %A in state %A" command state |> Error

    let (|Order|) leaseEvent = LeaseEvent.getOrder leaseEvent

    let isOrigin = function
        | Compacted _ -> true
        | _ -> false

    let compact (state:StreamState<LeaseEvent>) = Compacted (Array.ofList state.Events)

    let apply : Apply<LeaseEvent,LeaseState> =
        fun state -> function
            | Undid _ -> "Undid event should be filtered from lease events" |> Corrupt
            | Compacted _ -> "Compacted event should be filtered from lease events" |> Corrupt
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
            | PaymentScheduled ({ PaymentAmount = amount }, _) ->
                match state with
                | Outstanding data ->
                    let newTotalScheduled = data.TotalScheduled + amount
                    { data with
                        TotalScheduled = newTotalScheduled
                        AmountDue = newTotalScheduled - data.TotalPaid }
                    |> Outstanding
                | _ -> applyError PaymentScheduled state
            | PaymentReceived ({ PaymentAmount = amount }, _) ->
                match state with
                | Outstanding data ->
                    let newTotalPaid = data.TotalPaid + amount
                    { data with
                        TotalPaid = newTotalPaid
                        AmountDue = data.TotalScheduled - newTotalPaid }
                    |> Outstanding
                | _ -> applyError PaymentScheduled state
            | LeaseEvent.Terminated _ ->
                match state with    
                | Outstanding data ->
                    LeaseState.Terminated data
                | _ -> applyError PaymentScheduled state

    let decide : Decide<LeaseCommand,LeaseEvent,LeaseState> =
        fun (nextId: EventId) command state ->
            match command with
            | Undo _ -> Ok []
            | Create ({ StartDate = startDate } as lease) ->
                match state with
                | NonExistent -> 
                    let ctx = Context.create nextId (% startDate)
                    Created (lease, ctx) |> List.singleton |> Ok
                | _ -> commandError Create state
            | Modify (lease, effDate) ->
                match state with
                | Outstanding _ -> 
                    let ctx = Context.create nextId effDate
                    Modified (lease, ctx) |> List.singleton |> Ok
                | _ -> commandError Modify state
            | SchedulePayment ({ PaymentDate = pmtDate } as pmt) ->
                match state with
                | Outstanding _ -> 
                    let ctx = Context.create nextId %pmtDate
                    PaymentScheduled (pmt, ctx) |> List.singleton |> Ok
                | _ -> commandError SchedulePayment state
            | ReceivePayment ({ PaymentDate = pmtDate } as pmt) ->
                match state with
                | Outstanding _ -> 
                    let ctx = Context.create nextId %pmtDate
                    PaymentReceived (pmt, ctx) |> List.singleton |> Ok
                | _ -> commandError ReceivePayment state
            | Terminate effDate ->
                match state with
                | Outstanding _ -> 
                    let ctx = Context.create nextId effDate
                    LeaseEvent.Terminated ctx |> List.singleton |> Ok
                | _ -> commandError Terminate state

    let evolve : Evolve<LeaseEvent> =
        fun ({ NextId = nextId; Events = events } as state) event ->
            match event with
            | Undid undoEventId -> 
                let filteredEvents =
                    events
                    |> List.choose (fun e -> LeaseEvent.getEventId e |> Option.map (fun eventId -> (eventId, e)))
                    |> List.filter (fun (eventId, _) -> eventId <> undoEventId)
                    |> List.map snd
                { state with 
                    NextId = nextId + %1
                    Events = filteredEvents }
            | Compacted events ->
                { state with
                    Events = List.ofArray events }
            | _ -> 
                { state with 
                    NextId = nextId + %1
                    Events = event :: state.Events }

    let onOrBeforeObservationDate 
        observationDate 
        (effectiveDate: EffectiveDate, createdDate: CreatedDate) =
        match observationDate with
        | Latest -> true
        | AsOf asOfDate ->
            createdDate <= %asOfDate
        | AsAt asAtDate ->
            effectiveDate <= %asAtDate

    let reconstitute : Reconstitute<LeaseEvent,LeaseState> =
        fun observationDate events ->
            events
            |> List.choose (fun e -> LeaseEvent.getOrder e |> Option.map (fun o -> (o, e)))
            |> List.filter (fun (o, _) -> onOrBeforeObservationDate observationDate o)
            |> List.sortBy fst
            |> List.map snd
            |> List.fold apply NonExistent

    let interpret : Interpret<LeaseCommand,LeaseEvent> =
        let ok = Ok ()
        let onSuccess events = (ok, events)
        let onError msg = (Error msg, [])
        fun command { NextId = nextId; Events = events } ->
            let (|ObsDate|) (effDate: EffectiveDate) = %effDate |> AsAt
            let interpret' (ObsDate obsDate) command =
                reconstitute obsDate events
                |> decide nextId command
                |> Result.bimap onSuccess onError
            match command with
            | Undo undoEventId ->
                if 
                    events 
                    |> List.choose LeaseEvent.getEventId 
                    |> List.exists (fun eventId -> eventId = undoEventId)
                then 
                    (ok, [Undid undoEventId])
                else
                    let error = sprintf "Event with id %A does not exist" undoEventId |> Error
                    (error, [])
            | Create { StartDate = startDate } -> interpret' %startDate command
            | Modify (_, effDate) -> interpret' effDate command
            | SchedulePayment ({ PaymentDate = pmtDate }) -> interpret' %pmtDate command
            | ReceivePayment ({ PaymentDate = pmtDate }) -> interpret' %pmtDate command
            | Terminate effDate -> interpret' effDate command

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
    let create 
        (aggregate:Aggregate<LeaseCommand,LeaseEvent,LeaseState>) 
        (gateway:GesGateway) 
        (cache:Caching.Cache) 
        : Handler<LeaseId,LeaseCommand,LeaseEvent,LeaseState,'View> =
        let log = LoggerConfiguration().WriteTo.Console().CreateLogger()
        let accessStrategy = Equinox.EventStore.AccessStrategy.RollingSnapshots (aggregate.isOrigin, aggregate.compact)
        let cacheStrategy = Equinox.EventStore.CachingStrategy.SlidingWindow (cache, TimeSpan.FromMinutes 20.)
        let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
        let codec = Equinox.UnionCodec.JsonUtf8.Create<LeaseEvent>(serializationSettings)
        let initial = { NextId = %0; Events = [] }
        let fold = Seq.fold aggregate.evolve
        let resolve = GesResolver(gateway, codec, fold, initial, accessStrategy, cacheStrategy).Resolve
        let (|AggregateId|) (leaseId: LeaseId) = Equinox.AggregateId(aggregate.entity, LeaseId.toStringN leaseId)
        let (|Stream|) (AggregateId leaseId) = Equinox.Stream(log, resolve leaseId, 3)
        let execute (Stream stream) command = stream.Transact(aggregate.interpret command)
        let query : Query<LeaseId,LeaseEvent,'View> =
            fun (Stream stream) (obsDate:ObservationDate) (projection:Projection<LeaseEvent,'View>) -> 
                stream.Query(projection obsDate)
                |> AsyncResult.ofAsync
        { execute = execute
          query = query }
