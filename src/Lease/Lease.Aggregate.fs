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
      reconstitute: ObservationDate -> LeaseEvents -> LeaseState
      interpret: LeaseCommand -> LeaseEvents -> Result<unit,string> * LeaseEvent list
      fold: LeaseEvents -> seq<LeaseEvent> -> LeaseEvents }
module Aggregate =

    module EventContext =
        let create eventId effDate =
            { EventId = eventId
              EventCreatedDate = %DateTime.UtcNow
              EventEffectiveDate = effDate }
        let extractEventId (ctx:EventContext) = ctx.EventId
        let extractOrder (ctx:EventContext) = (ctx.EventEffectiveDate, ctx.EventCreatedDate)

    module LeaseEvent =
        let extractType : LeaseEvent -> EventType = function
            | Created _ -> %"Created"
            | PaymentScheduled _ -> %"PaymentScheduled" 
            | PaymentReceived _ -> %"PaymentReceived"
            | LeaseEvent.Terminated _ -> %"Terminated"
        let extractContext = function
            | Created e -> e.Context
            | PaymentScheduled e -> e.Context
            | PaymentReceived e -> e.Context
            | LeaseEvent.Terminated e -> e.Context
        let extractOrder = extractContext >> EventContext.extractOrder
        let extractEventId = extractContext >> EventContext.extractEventId
        let onOrBefore 
            observationDate 
            (leaseEvent:LeaseEvent) =
            let { EventEffectiveDate = effDate; EventCreatedDate = createdDate } = 
                leaseEvent |> extractContext
            match observationDate with
            | Latest -> true
            | AsOf asOfDate ->
                effDate <= %asOfDate
            | AsAt asAtDate ->
                createdDate <= %asAtDate

    module LeaseStateData =
        let init (ctx:EventContext) (lease:Lease) =
            { NextId = ctx.EventId + %1
              Events = []
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
        let updateEvents
            (event:LeaseEvent) =
            let eventType = event |> LeaseEvent.extractType
            let ctx = event |> LeaseEvent.extractContext
            fun (data:LeaseStateData) ->
                { data with
                    Events = (eventType, ctx) :: data.Events }
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
            | Created payload as event ->
                match state with
                | NonExistent ->
                    LeaseStateData.init payload.Context payload.Lease
                    |> LeaseStateData.updateEvents event
                    |> Outstanding
                | _ -> error Created state
            | PaymentScheduled payload as event ->
                match state with
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateAmounts payload.ScheduledPayment.ScheduledPaymentAmount %0m
                    |> LeaseStateData.updateContext payload.Context
                    |> LeaseStateData.updateEvents event
                    |> Outstanding
                | _ -> error PaymentScheduled state
            | PaymentReceived payload as event ->
                match state with
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateAmounts %0m payload.Payment.PaymentAmount
                    |> LeaseStateData.updateContext payload.Context
                    |> LeaseStateData.updateEvents event 
                    |> Outstanding
                | _ -> error PaymentReceived state
            | LeaseEvent.Terminated payload as event ->
                match state with    
                | Outstanding data ->
                    data
                    |> LeaseStateData.updateContext payload.Context
                    |> LeaseStateData.updateEvents event
                    |> LeaseState.Terminated
                | _ -> error LeaseEvent.Terminated state

    let interpretDomain 
        : LeaseCommand -> LeaseState -> Result<LeaseEvent list, string> =
        let error command state = sprintf "cannot execute %s in state %A" command state |> Error
        fun command state ->
            let ok e = e |> List.singleton |> Ok
            match command with
            | Create lease ->
                match state with
                | NonExistent -> 
                    let createdCtx = EventContext.create %0 %lease.StartDate
                    let paymentsScheduled =
                        DateTime.monthRange lease.StartDate lease.MaturityDate 
                        |> Seq.mapi (fun idx d ->
                            let eventId = idx + 1
                            let pmt =
                                { ScheduledPaymentDate = d
                                  ScheduledPaymentAmount = %lease.MonthlyPaymentAmount }
                            let ctx = EventContext.create %eventId %d 
                            {| ScheduledPayment = pmt; Context = ctx |}
                            |> PaymentScheduled)
                        |> Seq.toList
                    let created =
                        {| Lease = lease; Context = createdCtx |}
                        |> Created 
                    created :: paymentsScheduled
                    |> Ok
                | _ -> error "Create" state
            | SchedulePayment scheduledPayment ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = EventContext.create nextId %scheduledPayment.ScheduledPaymentDate
                    {| ScheduledPayment = scheduledPayment
                       Context = ctx |} 
                    |> PaymentScheduled
                    |> ok
                | _ -> error "SchedulePayment" state
            | ReceivePayment payment ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = EventContext.create nextId %payment.PaymentDate
                    {| Payment = payment
                       Context = ctx |} 
                    |> PaymentReceived
                    |> ok
                | _ -> error "ReceivePayment" state
            | Terminate effDate ->
                match state with
                | Outstanding { NextId = nextId } -> 
                    let ctx = EventContext.create nextId effDate
                    LeaseEvent.Terminated {| Context = ctx |} |> ok
                | _ -> error "Terminate" state

    let reconstitute 
        : ObservationDate -> LeaseEvents -> LeaseState =
        fun obsDate leaseEvents ->
            leaseEvents
            |> List.filter (LeaseEvent.onOrBefore obsDate)
            |> List.sortBy LeaseEvent.extractOrder
            |> List.fold evolveDomain NonExistent

    let interpret 
        : LeaseCommand -> LeaseEvents -> Result<unit,string> * LeaseEvent list =
        let ok = Ok ()
        let onSuccess events = (ok, events)
        let onError msg = (Error msg, [])
        fun command leaseEvents ->
            let (|ObsDate|) (effDate: EventEffectiveDate) = %effDate |> AsOf
            let decide (ObsDate obsDate) command =
                leaseEvents
                |> reconstitute obsDate
                |> interpretDomain command
                |> Result.bimap onSuccess onError
            match command with
            | Create { StartDate = startDate } -> decide %startDate command
            | SchedulePayment { ScheduledPaymentDate = pmtDate } -> decide %pmtDate command
            | ReceivePayment { PaymentDate = pmtDate } -> decide %pmtDate command
            | Terminate effDate -> decide effDate command

    let fold
        : LeaseEvents -> seq<LeaseEvent> -> LeaseEvents =
        Seq.fold (fun events event -> event :: events)

    let init () =
        { entity = "lease"
          initial = NonExistent
          evolveDomain = evolveDomain
          interpretDomain = interpretDomain
          reconstitute = reconstitute
          interpret = interpret
          fold = fold }
