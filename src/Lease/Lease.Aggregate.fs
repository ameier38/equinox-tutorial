namespace Lease

open FSharp.UMX
open System

type ObservationDate =
    | Latest
    | AsOf of DateTime
    | AsAt of DateTime

type Apply<'DomainEvent,'DomainState> =
    'DomainState
     -> 'DomainEvent
     -> 'DomainState

type Decide<'DomainCommand,'DomainEvent,'DomainState> =
    EventId
     -> 'DomainCommand
     -> 'DomainState
     -> Result<'DomainEvent list,string>

type StreamState<'DomainEvent> = 
    { NextId: EventId 
      Events: 'DomainEvent list }

type Reconstitute<'DomainEvent,'DomainState> =
    ObservationDate
     -> 'DomainEvent list
     -> 'DomainState

type Evolve<'DomainEvent> = 
    StreamState<'DomainEvent>
     -> 'DomainEvent
     -> StreamState<'DomainEvent>

type Interpret<'DomainCommand,'DomainEvent> = 
    'DomainCommand 
     -> StreamState<'DomainEvent>
     -> Result<unit,string> * 'DomainEvent list

type IsOrigin<'DomainEvent> = 
    'DomainEvent
     -> bool

type Compact<'DomainEvent> = 
    StreamState<'DomainEvent>
     -> 'DomainEvent

type Execute<'EntityId,'DomainCommand> =
    'EntityId
     -> 'DomainCommand 
     -> AsyncResult<unit,string>

type Projection<'DomainEvent,'View> =
    ObservationDate
     -> StreamState<'DomainEvent>
     -> 'View

type Query<'EntityId,'DomainEvent,'View> = 
    'EntityId
     -> ObservationDate 
     -> Projection<'DomainEvent,'View>
     -> AsyncResult<'View,string>

type Aggregate<'DomainCommand,'DomainEvent,'DomainState> =
    { entity: string
      initial: 'DomainState
      isOrigin: IsOrigin<'DomainEvent>
      apply: Apply<'DomainEvent,'DomainState>
      decide: Decide<'DomainCommand,'DomainEvent,'DomainState>
      reconstitute: Reconstitute<'DomainEvent,'DomainState>
      compact: Compact<'DomainEvent>
      evolve: Evolve<'DomainEvent>
      interpret: Interpret<'DomainCommand,'DomainEvent> }
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
            | Created { Lease = lease; Context = ctx } ->
                match state with
                | NonExistent ->
                    { Lease = lease
                      TotalScheduled = 0m
                      TotalPaid = 0m
                      AmountDue = 0m
                      CreatedDate = %ctx.CreatedDate
                      UpdatedDate = %ctx.CreatedDate }
                    |> Outstanding
                | _ -> applyError Created state
            | Modified { Lease = lease; Context = ctx } ->
                match state with
                | Outstanding data ->
                    { data with
                        Lease = lease
                        UpdatedDate = %ctx.CreatedDate } 
                    |> Outstanding
                | _ -> applyError Modified state
            | PaymentScheduled { Payment = { PaymentAmount = amount }; Context = ctx } ->
                match state with
                | Outstanding data ->
                    let newTotalScheduled = data.TotalScheduled + amount
                    { data with
                        TotalScheduled = newTotalScheduled
                        AmountDue = newTotalScheduled - data.TotalPaid
                        UpdatedDate = %ctx.CreatedDate }
                    |> Outstanding
                | _ -> applyError PaymentScheduled state
            | PaymentReceived { Payment = { PaymentAmount = amount }; Context = ctx} ->
                match state with
                | Outstanding data ->
                    let newTotalPaid = data.TotalPaid + amount
                    { data with
                        TotalPaid = newTotalPaid
                        AmountDue = data.TotalScheduled - newTotalPaid
                        UpdatedDate = %ctx.CreatedDate }
                    |> Outstanding
                | _ -> applyError PaymentReceived state
            | LeaseEvent.Terminated ctx ->
                match state with    
                | Outstanding data ->
                    { data with
                        UpdatedDate = %ctx.CreatedDate }
                    |> LeaseState.Terminated
                | _ -> applyError LeaseEvent.Terminated state

    let decide : Decide<LeaseCommand,LeaseEvent,LeaseState> =
        fun (nextId: EventId) command state ->
            match command with
            | Undo _ -> Ok []
            | Create ({ StartDate = startDate } as lease) ->
                match state with
                | NonExistent -> 
                    let ctx = Context.create nextId (% startDate)
                    Created { Lease = lease; Context = ctx } |> List.singleton |> Ok
                | _ -> commandError Create state
            | Modify (lease, effDate) ->
                match state with
                | Outstanding _ -> 
                    let ctx = Context.create nextId effDate
                    Modified { Lease = lease; Context = ctx } |> List.singleton |> Ok
                | _ -> commandError Modify state
            | SchedulePayment ({ PaymentDate = pmtDate } as pmt) ->
                match state with
                | Outstanding _ -> 
                    let ctx = Context.create nextId %pmtDate
                    PaymentScheduled { Payment = pmt; Context = ctx } |> List.singleton |> Ok
                | _ -> commandError SchedulePayment state
            | ReceivePayment ({ PaymentDate = pmtDate } as pmt) ->
                match state with
                | Outstanding _ -> 
                    let ctx = Context.create nextId %pmtDate
                    PaymentReceived { Payment = pmt; Context = ctx } |> List.singleton |> Ok
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

    let leaseAggregate =
        { entity = "lease"
          initial = NonExistent
          isOrigin = isOrigin
          apply = apply
          decide = decide
          reconstitute = reconstitute
          compact = compact
          evolve = evolve
          interpret = interpret }