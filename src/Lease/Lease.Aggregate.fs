namespace Lease

open FSharp.UMX
open System

type ObservationDate =
    | Latest
    | AsOf of DateTime
    | AsAt of DateTime

type EvolveDomain<'DomainEvent,'DomainState> =
    'DomainState
     -> 'DomainEvent
     -> 'DomainState

type InterpretDomain<'DomainCommand,'DomainEvent,'DomainState> =
    'DomainCommand
     -> 'DomainState
     -> Result<'DomainEvent list,string>

type EffectiveEvents<'DomainEvent> = 'DomainEvent list

type Reconstitute<'DomainEvent,'DomainState> =
    ObservationDate
     -> EffectiveEvents<'DomainEvent>
     -> 'DomainState

type Evolve<'DomainEvent> = 
    EffectiveEvents<'DomainEvent>
     -> 'DomainEvent
     -> EffectiveEvents<'DomainEvent>

type Interpret<'DomainCommand,'DomainEvent> = 
    'DomainCommand 
     -> EffectiveEvents<'DomainEvent>
     -> Result<unit,string> * 'DomainEvent list

type Execute<'EntityId,'DomainCommand> =
    'EntityId
     -> 'DomainCommand 
     -> AsyncResult<unit,string>

type Projection<'DomainEvent,'View> =
    ObservationDate
     -> EffectiveEvents<'DomainEvent>
     -> 'View

type Query<'EntityId,'DomainEvent,'View> = 
    'EntityId
     -> ObservationDate 
     -> Projection<'DomainEvent,'View>
     -> AsyncResult<'View,string>

type Aggregate<'DomainCommand,'DomainEvent,'DomainState> =
    { entity: string
      initial: 'DomainState
      evolveDomain: EvolveDomain<'DomainEvent,'DomainState>
      interpretDomain: InterpretDomain<'DomainCommand,'DomainEvent,'DomainState>
      reconstitute: Reconstitute<'DomainEvent,'DomainState>
      evolve: Evolve<'DomainEvent>
      interpret: Interpret<'DomainCommand,'DomainEvent> }
module Aggregate =

    module Context =
        let create eventId effDate =
            { EventId = eventId
              EventCreatedDate = %DateTime.UtcNow
              EventEffectiveDate = effDate }

    module LeaseEvent =
        let (|Order|) { EventEffectiveDate = effDate; EventCreatedDate = createdDate } = (effDate, createdDate)
        let tryGetContext = function
            | Undid _ -> None
            | Created e -> e.Context |> Some
            | Modified e -> e.Context |> Some
            | PaymentScheduled e -> e.Context |> Some
            | PaymentReceived e -> e.Context |> Some
            | LeaseEvent.Terminated e -> e.Context |> Some
        let tryGetOrder = tryGetContext >> Option.map (fun (Order order) -> order)
        let tryGetEventId = tryGetContext >> Option.map (fun { EventId = eventId } -> eventId)

    module LeaseStateData =
        let init (ctx:EventContext) (lease:Lease) =
            { NextId = ctx.EventId + %1
              Lease = lease
              TotalScheduled = %0m
              TotalPaid = %0m
              AmountDue = 0m
              CreatedDate = %ctx.EventCreatedDate
              UpdatedDate = %ctx.EventCreatedDate }
        let updateContext
            (ctx:EventContext) =
            fun (data:LeaseStateData) ->
                { data with
                    NextId = ctx.EventId + %1
                    UpdatedDate = %ctx.EventCreatedDate }
        let updateLease 
            (lease:Lease) =
            fun (data:LeaseStateData) ->
                { data with
                    Lease = lease }
        let updateAmounts
            (scheduledPaymentAmount:ScheduledPaymentAmount)
            (paymentAmount:PaymentAmount) =
            fun (data:LeaseStateData) ->
                let totalScheduled = data.TotalScheduled + scheduledPaymentAmount
                let totalPaid = data.TotalPaid + paymentAmount
                let amountDue = %totalScheduled - %totalPaid
                { data with
                    TotalScheduled = totalScheduled
                    TotalPaid = totalPaid
                    AmountDue = amountDue }

    let applyError event state = sprintf "%A cannot be applied to state %A" event state |> Corrupt

    let commandError command state = sprintf "cannot execte %A in state %A" command state |> Error

    let (|Order|) leaseEvent = LeaseEvent.tryGetOrder leaseEvent

    let evolveDomain : EvolveDomain<LeaseEvent,LeaseState> =
        fun state -> function
            | Undid _ -> "Undid event should be filtered from effective events" |> Corrupt
            | Created e ->
                match state with
                | NonExistent ->
                    LeaseStateData.init e.Context e.Lease
                    |> Outstanding
                | _ -> applyError Created state
            | Modified e ->
                match state with
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateLease e.Lease
                    |> LeaseStateData.updateContext e.Context
                    |> Outstanding
                | _ -> applyError Modified state
            | PaymentScheduled e ->
                match state with
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateAmounts e.ScheduledPayment.ScheduledPaymentAmount %0m
                    |> LeaseStateData.updateContext e.Context
                    |> Outstanding
                | _ -> applyError PaymentScheduled state
            | PaymentReceived e ->
                match state with
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateAmounts %0m e.Payment.PaymentAmount
                    |> LeaseStateData.updateContext e.Context
                    |> Outstanding
                | _ -> applyError PaymentReceived state
            | LeaseEvent.Terminated e ->
                match state with    
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateContext e.Context
                    |> LeaseState.Terminated
                | _ -> applyError LeaseEvent.Terminated state

    let interpretDomain : InterpretDomain<LeaseCommand,LeaseEvent,LeaseState> =
        fun command state ->
            let ok e = e |> List.singleton |> Ok
            match command with
            | Undo _ -> Ok []
            | Create lease ->
                match state with
                | NonExistent -> 
                    let createdCtx = Context.create %0 %lease.StartDate
                    let paymentsScheduled =
                        DateTime.monthRange lease.StartDate lease.MaturityDate 
                        |> Seq.imap (fun (idx, d) ->
                            let pmt =
                                { ScheduledPaymentDate = d
                                  ScheduledPaymentAmount = %lease.MonthlyPaymentAmount }
                            let ctx = Context.create %d 
                            {| ScheduledPayment = pmt|}}
                            |> PaymentScheduled)
                        |> Seq.toList
                    {| Lease = lease
                       ScheduledPayments = scheduledPayments
                       Context = ctx |}
                    |> Created 
                    |> ok
                | _ -> commandError Create state
            | Modify (lease, effDate) ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = Context.create nextId effDate
                    let scheduledPayments =
                        DateTime.monthRange lease.StartDate lease.MaturityDate 
                        |> Seq.map (fun d ->
                            { ScheduledPaymentDate = d
                              ScheduledPaymentAmount = %lease.MonthlyPaymentAmount })
                        |> Seq.toList
                    {| Lease = lease
                       ScheduledPayments = scheduledPayments
                       Context = ctx |} 
                    |> Modified
                    |> ok
                | _ -> commandError Modify state
            | SchedulePayment scheduledPayment ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = Context.create nextId %scheduledPayment.ScheduledPaymentDate
                    {| ScheduledPayment = scheduledPayment
                       Context = ctx |} 
                    |> PaymentScheduled
                    |> ok
                | _ -> commandError SchedulePayment state
            | ReceivePayment payment ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = Context.create nextId %payment.PaymentDate
                    {| Payment = payment
                       Context = ctx |} 
                    |> PaymentReceived
                    |> ok
                | _ -> commandError ReceivePayment state
            | Terminate effDate ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = Context.create nextId effDate
                    LeaseEvent.Terminated {| Context = ctx |} |> ok
                | _ -> commandError Terminate state

    let evolve : Evolve<LeaseEvent> =
        fun effectiveEvents event ->
            match event with
            | Undid undoEventId -> 
                effectiveEvents
                |> List.choose (fun e -> LeaseEvent.tryGetEventId e |> Option.map (fun eventId -> (eventId, e)))
                |> List.filter (fun (eventId, _) -> eventId <> undoEventId)
                |> List.map snd
            | _ -> effectiveEvents

    let onOrBeforeObservationDate 
        observationDate 
        (effDate: EventEffectiveDate, createdDate: EventCreatedDate) =
        match observationDate with
        | Latest -> true
        | AsOf asOfDate ->
            effDate <= %asOfDate
        | AsAt asAtDate ->
            createdDate <= %asAtDate

    let reconstitute : Reconstitute<LeaseEvent,LeaseState> =
        fun observationDate effectiveEvents ->
            effectiveEvents
            |> List.choose (fun e -> LeaseEvent.tryGetOrder e |> Option.map (fun o -> (o, e)))
            |> List.filter (fun (o, _) -> onOrBeforeObservationDate observationDate o)
            |> List.sortBy fst
            |> List.map snd
            |> List.fold evolveDomain NonExistent

    let interpret : Interpret<LeaseCommand,LeaseEvent> =
        let ok = Ok ()
        let onSuccess events = (ok, events)
        let onError msg = (Error msg, [])
        fun command effectiveEvents ->
            let (|ObsDate|) (effDate: EventEffectiveDate) = %effDate |> AsOf
            let decide (ObsDate obsDate) command =
                reconstitute obsDate effectiveEvents
                |> interpretDomain command
                |> Result.bimap onSuccess onError
            match command with
            | Undo undoEventId ->
                if 
                    effectiveEvents 
                    |> List.choose LeaseEvent.tryGetEventId 
                    |> List.exists (fun eventId -> eventId = undoEventId)
                then 
                    (ok, [Undid undoEventId])
                else
                    let error = sprintf "Event with id %A does not exist" undoEventId |> Error
                    (error, [])
            | Create { StartDate = startDate } -> decide %startDate command
            | Modify (_, effDate) -> decide effDate command
            | SchedulePayment { ScheduledPaymentDate = pmtDate } -> decide %pmtDate command
            | ReceivePayment { PaymentDate = pmtDate } -> decide %pmtDate command
            | Terminate effDate -> decide effDate command

    let leaseAggregate =
        { entity = "lease"
          initial = NonExistent
          evolveDomain = evolveDomain
          interpretDomain = interpretDomain
          reconstitute = reconstitute
          evolve = evolve
          interpret = interpret }