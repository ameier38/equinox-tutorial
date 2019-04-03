module Lease.Aggregate

open FSharp.UMX
open System

module EventContext =
    let create effDate =
        { EventEffectiveDate = effDate
          EventCreatedDate = %DateTime.UtcNow }

module LeaseEvent =
    let getEventEffectiveOrder : LeaseEvent -> EventEffectiveOrder = function
        | LeaseCreated _ -> %1
        | PaymentScheduled _ -> %2
        | PaymentReceived _ -> %3
        | LeaseTerminated _ -> %4
    let getEventContext = function
        | LeaseCreated payload -> payload.Context
        | PaymentScheduled payload
        | PaymentReceived payload -> payload.Context
        | LeaseTerminated payload -> payload.Context
    let onOrBefore 
        (asOfDate:AsOfDate)
        (effOrderOpt:EventEffectiveOrder option) =
        fun (event:LeaseEvent) ->
            let { EventCreatedDate = eventCreatedDate
                  EventEffectiveDate = eventEffectiveDate } = getEventContext event
            let eventEffectiveOrder = getEventEffectiveOrder event
            match (asOfDate, effOrderOpt) with
            | AsOn asOnDate, Some effOrder when %eventEffectiveDate = asOnDate ->
                eventEffectiveOrder <= effOrder
            | AsOn asOnDate, _ ->
                %eventEffectiveDate <= asOnDate
            | AsAt asAtDate, Some effOrder when %eventEffectiveDate = asAtDate ->
                (eventEffectiveOrder <= effOrder) && (%eventCreatedDate <= asAtDate)
            | AsAt asAtDate, _ ->
                (%eventCreatedDate <= asAtDate) && (%eventEffectiveDate <= asAtDate)

module LeaseState =
    let map f = function
        | Corrupt err -> Corrupt err
        | Nonexistent -> Nonexistent
        | Outstanding obs -> f obs |> Outstanding
        | Terminated obs -> f obs |> Terminated

module LeaseObservation =
    let init (lease:Lease) =
        { Lease = lease
          TotalScheduled = 0m<usd>
          TotalPaid = 0m<usd>
          AmountDue = 0m<usd>
          ScheduledPaymentIds = []
          ReceivedPaymentIds = [] }

module Observers =
    let observeLeaseCreated
        (lease: Lease) =
        function
        | Corrupt err -> Corrupt err
        | Nonexistent -> LeaseObservation.init lease |> Outstanding
        | _ -> "cannot observe LeaseCreated; lease already exists" |> Corrupt
    let observePaymentScheduled (payment:Payment) =
        let schedulePayment (obs:LeaseObservation) =
            let totalScheduled = obs.TotalScheduled + payment.PaymentAmount
            let amountDue = totalScheduled - obs.TotalPaid
            { obs with
                TotalScheduled = totalScheduled
                AmountDue = amountDue }
        function
        | Nonexistent -> "cannot observe PaymentScheduled; lease does not exist" |> Corrupt
        | state -> state |> LeaseState.map schedulePayment
    let observePaymentReceived (payment:Payment) =
        let receivePayment (obs:LeaseObservation) =
            let totalPaid = obs.TotalPaid + payment.PaymentAmount
            let amountDue = obs.TotalScheduled - totalPaid
            { obs with
                TotalPaid = totalPaid
                AmountDue = amountDue }
        function
        | Nonexistent -> "cannot observe PaymentReceived; lease does not exist" |> Corrupt
        | state -> state |> LeaseState.map receivePayment
    let observeLeaseTerminated =
        let terminateLease (obs:LeaseObservation) =
            { obs with
                TotalScheduled = obs.TotalPaid
                AmountDue = 0m<usd> }
        function
        | Corrupt err -> Corrupt err
        | Nonexistent -> "cannot observe LeaseTerminated; lease does not exist" |> Corrupt
        | Terminated _ -> "cannot observe LeaseTerminated; lease is already terminated" |> Corrupt
        | Outstanding obs -> terminateLease obs |> Terminated

module Deciders =
    let success event = event |> List.singleton |> Ok
    let failure err = Error err
    let decideToCreateLease effDate lease =
        function
        | Corrupt err -> failure err
        | Nonexistent ->
            let ctx = EventContext.create effDate
            {| Context = ctx; Lease = lease |}
            |> LeaseCreated
            |> success
        | _ -> "cannot create lease; lease already exists" |> failure
    let decideToSchedulePayment effDate payment =
        function
        | Corrupt err -> failure err
        | Nonexistent -> "cannot schedule payment; lease does not exist" |> failure
        | Outstanding obs ->
            let paymentId = payment.PaymentId
            if obs.ScheduledPaymentIds |> List.contains paymentId then
                paymentId
                |> PaymentId.toStringN
                |> sprintf "payment %s already scheduled"
                |> failure
            else
                let ctx = EventContext.create effDate
                {| Context = ctx; Payment = payment |}
                |> PaymentScheduled
                |> success
        | Terminated _ -> "cannot schedule payment; lease is terminated" |> failure
    let decideToReceivePayment effDate payment =
        function
        | Corrupt err -> failure err
        | Nonexistent -> "cannot receive payment; lease does not exist" |> failure
        | Outstanding obs
        | Terminated obs ->
            let paymentId = payment.PaymentId
            if obs.ReceivedPaymentIds |> List.contains paymentId then
                paymentId
                |> PaymentId.toStringN
                |> sprintf "payment %s already received"
                |> failure
            else
                let ctx = EventContext.create effDate
                {| Context = ctx; Payment = payment |}
                |> PaymentReceived
                |> success
    let decideToTerminateLease effDate =
        function
        | Corrupt err -> failure err
        | Nonexistent -> "cannot terminate lease; lease does not exist" |> failure
        | Terminated _ -> "cannot terminate lease; lease is already terminated" |> failure
        | Outstanding _ ->
            let ctx = EventContext.create effDate
            {| Context = ctx |}
            |> LeaseTerminated
            |> success

let observe 
    : LeaseState -> LeaseEvent -> LeaseState =
    fun state -> function
        | LeaseCreated payload -> Observers.observeLeaseCreated payload.Lease state
        | PaymentScheduled payload -> Observers.observePaymentScheduled payload.Payment state
        | PaymentReceived payload -> Observers.observePaymentReceived payload.Payment state
        | LeaseTerminated _ -> Observers.observeLeaseTerminated state

let decide 
    : LeaseCommand -> LeaseState -> Result<LeaseEvent list, string> =
    fun command state ->
        match command with
        | CreateLease (effDate, lease) -> Deciders.decideToCreateLease effDate lease state
        | SchedulePayment (effDate, payment) -> Deciders.decideToSchedulePayment effDate payment state
        | ReceivePayment (effDate, payment) -> Deciders.decideToReceivePayment effDate payment state
        | TerminateLease effDate -> Deciders.decideToTerminateLease effDate state

let reconstitute
    : AsOfDate -> LeaseEvents -> LeaseState =
    fun asOfDate leaseEvents ->
        leaseEvents
        |> List.filter (LeaseEvent.onOrBefore asOfDate)
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
