namespace Lease

open FSharp.UMX
open System

type ObservationDate =
    | Latest
    | AsOf of DateTime
    | AsAt of DateTime

type Aggregate =
    { entity: string
      initial: LeaseState
      evolveDomain: LeaseState -> LeaseEvent -> LeaseState
      interpretDomain: LeaseCommand -> LeaseState -> Result<LeaseEvent list,string>
      filterAtOrBefore: ObservationDate -> EffectiveLeaseEvents -> EffectiveLeaseEvents
      reconstitute: EffectiveLeaseEvents -> LeaseState
      evolve: EffectiveLeaseEvents -> LeaseEvent -> EffectiveLeaseEvents
      interpret: LeaseCommand -> EffectiveLeaseEvents -> Result<unit,string> * LeaseEvent list }
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

    let evolveDomain 
        : LeaseState -> LeaseEvent -> LeaseState =
        let error event state = sprintf "%A cannot be applied to state %A" event state |> Corrupt
        fun state -> function
            | Undid _ -> "Undid event should be filtered from effective events" |> Corrupt
            | Created e ->
                match state with
                | NonExistent ->
                    LeaseStateData.init e.Context e.Lease
                    |> Outstanding
                | _ -> error Created state
            | PaymentScheduled e ->
                match state with
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateAmounts e.ScheduledPayment.ScheduledPaymentAmount %0m
                    |> LeaseStateData.updateContext e.Context
                    |> Outstanding
                | _ -> error PaymentScheduled state
            | PaymentReceived e ->
                match state with
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateAmounts %0m e.Payment.PaymentAmount
                    |> LeaseStateData.updateContext e.Context
                    |> Outstanding
                | _ -> error PaymentReceived state
            | LeaseEvent.Terminated e ->
                match state with    
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateContext e.Context
                    |> LeaseState.Terminated
                | _ -> error LeaseEvent.Terminated state

    let interpretDomain 
        : LeaseCommand -> LeaseState -> Result<LeaseEvent list, string> =
        let error command state = sprintf "cannot execute %A in state %A" command state |> Error
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
                        |> Seq.mapi (fun idx d ->
                            let eventId = idx + 1
                            let pmt =
                                { ScheduledPaymentDate = d
                                  ScheduledPaymentAmount = %lease.MonthlyPaymentAmount }
                            let ctx = Context.create %eventId %d 
                            {| ScheduledPayment = pmt; Context = ctx |}
                            |> PaymentScheduled)
                        |> Seq.toList
                    let created =
                        {| Lease = lease; Context = createdCtx |}
                        |> Created 
                    created :: paymentsScheduled
                    |> Ok
                | _ -> error Create state
            | SchedulePayment scheduledPayment ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = Context.create nextId %scheduledPayment.ScheduledPaymentDate
                    {| ScheduledPayment = scheduledPayment
                       Context = ctx |} 
                    |> PaymentScheduled
                    |> ok
                | _ -> error SchedulePayment state
            | ReceivePayment payment ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = Context.create nextId %payment.PaymentDate
                    {| Payment = payment
                       Context = ctx |} 
                    |> PaymentReceived
                    |> ok
                | _ -> error ReceivePayment state
            | Terminate effDate ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = Context.create nextId effDate
                    LeaseEvent.Terminated {| Context = ctx |} |> ok
                | _ -> error Terminate state

    let evolve 
        : EffectiveLeaseEvents -> LeaseEvent -> EffectiveLeaseEvents =
        fun effectiveEvents event ->
            match event with
            | Undid e -> 
                effectiveEvents
                |> List.choose (fun e -> LeaseEvent.tryGetEventId e |> Option.map (fun eventId -> (eventId, e)))
                |> List.filter (fun (eventId, _) -> eventId <> e.EventId)
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

    let filterAtOrBefore
        : ObservationDate -> LeaseEvent list -> EffectiveLeaseEvents =
        fun observationDate effectiveEvents ->
            effectiveEvents
            |> List.choose (fun e -> LeaseEvent.tryGetOrder e |> Option.map (fun o -> (o, e)))
            |> List.filter (fun (o, _) -> onOrBeforeObservationDate observationDate o)
            |> List.map snd

    let reconstitute 
        : EffectiveLeaseEvents -> LeaseState =
        fun effectiveEvents ->
            effectiveEvents
            |> List.choose (fun e -> LeaseEvent.tryGetOrder e |> Option.map (fun o -> (o, e)))
            |> List.sortBy fst
            |> List.map snd
            |> List.fold evolveDomain NonExistent

    let interpret 
        : LeaseCommand -> EffectiveLeaseEvents -> Result<unit,string> * LeaseEvent list =
        let ok = Ok ()
        let onSuccess events = (ok, events)
        let onError msg = (Error msg, [])
        fun command effectiveEvents ->
            let (|ObsDate|) (effDate: EventEffectiveDate) = %effDate |> AsOf
            let decide (ObsDate obsDate) command =
                effectiveEvents
                |> filterAtOrBefore obsDate
                |> reconstitute
                |> interpretDomain command
                |> Result.bimap onSuccess onError
            match command with
            | Undo undoEventId ->
                if 
                    effectiveEvents 
                    |> List.choose LeaseEvent.tryGetEventId 
                    |> List.exists (fun eventId -> eventId = undoEventId)
                then 
                    (ok, [Undid {| EventId = undoEventId |}])
                else
                    let error = sprintf "Event with id %A does not exist" undoEventId |> Error
                    (error, [])
            | Create { StartDate = startDate } -> decide %startDate command
            | SchedulePayment { ScheduledPaymentDate = pmtDate } -> decide %pmtDate command
            | ReceivePayment { PaymentDate = pmtDate } -> decide %pmtDate command
            | Terminate effDate -> decide effDate command

    let leaseAggregate =
        { entity = "lease"
          initial = NonExistent
          evolveDomain = evolveDomain
          interpretDomain = interpretDomain
          filterAtOrBefore = filterAtOrBefore
          reconstitute = reconstitute
          evolve = evolve
          interpret = interpret }