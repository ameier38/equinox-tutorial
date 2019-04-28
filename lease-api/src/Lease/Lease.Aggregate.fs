module Lease.Aggregate

open FSharp.UMX
open System

module EventContext =
    let create effDate eventId =
        { EventId = eventId
          EventEffectiveDate = effDate
          EventCreatedDate = %DateTime.UtcNow }

module StoredEvent =
    let tryToDeletedEvent = function
        | StoredEvent.EventDeleted payload ->
            Some (payload.EventContext.EventCreatedDate, payload.EventId)
        | _ -> None
    let tryToLeaseEvent = function
        | StoredEvent.EventDeleted _ -> None
        | StoredEvent.LeaseCreated payload ->
            (payload.EventContext, payload.NewLease)
            |> LeaseEvent.LeaseCreated
            |> Some
        | StoredEvent.PaymentScheduled payload ->
            (payload.EventContext, payload.ScheduledAmount)
            |> LeaseEvent.PaymentScheduled
            |> Some
        | StoredEvent.PaymentReceived payload ->
            (payload.EventContext, payload.ReceivedAmount)
            |> LeaseEvent.PaymentReceived
            |> Some
        | StoredEvent.LeaseTerminated payload ->
            payload.EventContext
            |> LeaseEvent.LeaseTerminated
            |> Some

module LeaseEvent =
    let getEventType : LeaseEvent -> EventType = function
        | LeaseEvent.LeaseCreated _ -> %"LeaseCreated"
        | LeaseEvent.PaymentScheduled _ -> %"PaymentScheduled"
        | LeaseEvent.PaymentReceived _ -> %"PaymentReceived"
        | LeaseEvent.LeaseTerminated _ -> %"LeaseTerminated"
    let getEventEffectiveOrder : LeaseEvent -> EventEffectiveOrder = function
        | LeaseEvent.LeaseCreated _ -> %1
        | LeaseEvent.PaymentScheduled _ -> %2
        | LeaseEvent.PaymentReceived _ -> %3
        | LeaseEvent.LeaseTerminated _ -> %4
    let getEventContext = function
        | LeaseEvent.LeaseCreated (ctx, _)
        | LeaseEvent.PaymentScheduled (ctx, _)
        | LeaseEvent.PaymentReceived (ctx, _)
        | LeaseEvent.LeaseTerminated ctx -> ctx
    let getOrder leaseEvent = 
        let ctx = getEventContext leaseEvent
        let effOrder = getEventEffectiveOrder leaseEvent
        ctx.EventEffectiveDate, effOrder, ctx.EventCreatedDate
    let asOfOrBefore 
        ({ AsAt = createdDate; AsOn = effDate })
        (effOrderOpt:EventEffectiveOrder option) =
        fun (event:LeaseEvent) ->
            let { EventCreatedDate = eventCreatedDate
                  EventEffectiveDate = eventEffectiveDate } = getEventContext event
            let eventEffectiveOrder = getEventEffectiveOrder event
            match effOrderOpt with
            | Some effOrder when eventEffectiveDate = effDate ->
                (eventEffectiveOrder <= effOrder) && (eventCreatedDate <= createdDate)
            | _ ->
                (eventEffectiveDate <= effDate) && (eventCreatedDate <= createdDate)
    let toStoredEvent = function
        | LeaseEvent.LeaseCreated (ctx, newLease) ->
            {| EventContext = ctx; NewLease = newLease |}
            |> StoredEvent.LeaseCreated
        | LeaseEvent.PaymentScheduled (ctx, scheduledAmount) ->
            {| EventContext = ctx; ScheduledAmount = scheduledAmount |}
            |> StoredEvent.PaymentScheduled
        | LeaseEvent.PaymentReceived (ctx, receivedAmount) ->
            {| EventContext = ctx; ReceivedAmount = receivedAmount |}
            |> StoredEvent.PaymentReceived
        | LeaseEvent.LeaseTerminated ctx ->
            {| EventContext = ctx |}
            |> StoredEvent.LeaseTerminated

module LeaseObservation =
    let createLease 
        (effDate:EventEffectiveDate)
        (newLease:NewLease) =
        { LeaseId = newLease.LeaseId
          UserId = newLease.UserId
          StartDate = %effDate
          MaturityDate = %newLease.MaturityDate
          MonthlyPaymentAmount = newLease.MonthlyPaymentAmount
          TotalScheduled = 0m<usd>
          TotalPaid = 0m<usd>
          AmountDue = 0m<usd>
          LeaseStatus = Outstanding }
    let schedulePayment 
        (paymentAmount:USD) =
        fun leaseObs ->
            let totalScheduled = leaseObs.TotalScheduled + paymentAmount
            let amountDue = totalScheduled - leaseObs.TotalPaid
            { leaseObs with
                TotalScheduled = totalScheduled
                AmountDue = amountDue }
    let receivePayment 
        (paymentAmount:USD) =
        fun leaseObs ->
            let totalPaid = leaseObs.TotalPaid + paymentAmount
            let amountDue = leaseObs.TotalScheduled - totalPaid
            { leaseObs with
                TotalPaid = totalPaid
                AmountDue = amountDue }
    let terminateLease =
        fun leaseObs ->
            { leaseObs with
                LeaseStatus = Terminated }

let observe 
    : LeaseState -> LeaseEvent -> LeaseState =
    fun state -> function
        | LeaseEvent.LeaseCreated ({ EventEffectiveDate = effDate }, newLease) ->
            state
            |> Result.bind (function
                | None -> LeaseObservation.createLease effDate newLease |> Some |> Ok
                | Some _ -> Error "cannot observe LeaseCreated; lease already exists")
        | LeaseEvent.PaymentScheduled (_, paymentAmount) ->
            state
            |> Result.bind(function
                | None -> Error "cannot observe PaymentScheduled; lease does not exist"
                | Some leaseObs -> leaseObs |> LeaseObservation.schedulePayment paymentAmount |> Some |> Ok)
        | LeaseEvent.PaymentReceived (_, paymentAmount) ->
            state
            |> Result.bind(function
                | None -> Error "cannot observe PaymentReceived; lease does not exist"
                | Some leaseObs -> leaseObs |> LeaseObservation.receivePayment paymentAmount |> Some |> Ok)
        | LeaseEvent.LeaseTerminated _ ->
            state
            |> Result.bind(function
                | None -> Error "cannot observe LeaseTerminated"
                | Some leaseObs -> leaseObs |> LeaseObservation.terminateLease |> Some |> Ok)
            

module StreamState =
    let getEffectiveLeaseEvents
        ({ AsAt = createdDate } as asOfDate)
        (effOrderOpt:EventEffectiveOrder option) =
        fun { LeaseEvents = leaseEvents; DeletedEvents = deletedEvents } ->
            let deletedEventIds =
                deletedEvents
                |> List.choose (fun (deletedDate, deletedEventId) ->
                    if deletedDate <= createdDate then Some deletedEventId
                    else None)
            leaseEvents
            |> List.filter (fun leaseEvent ->
                let { EventId = eventId } = leaseEvent |> LeaseEvent.getEventContext
                not (deletedEventIds |> List.contains eventId))
            |> List.filter (LeaseEvent.asOfOrBefore asOfDate effOrderOpt)

let reconstitute
    (asOfDate:AsOfDate)
    (effOrderOpt: EventEffectiveOrder option) =
    fun streamState ->
        streamState
        |> StreamState.getEffectiveLeaseEvents asOfDate effOrderOpt
        |> List.sortBy LeaseEvent.getOrder
        |> List.fold observe (Ok None)

let decide
    (command:Command)
    : StreamState -> Result<unit,string> * StoredEvent list =
    let ok = Ok ()
    let success (storedEvent:StoredEvent) = (ok, [storedEvent])
    let failure msg = (Error msg, [])
    fun streamState ->
        match command with
        | DeleteEvent eventId ->
            let deletedEventIds =
                streamState.DeletedEvents
                |> List.map (fun (_, eventId) -> eventId)
            if deletedEventIds |> List.contains eventId then
                sprintf "eventId %d already deleted" eventId |> failure
            else
                {| EventContext = {| EventCreatedDate = %DateTime.UtcNow |} ; EventId = eventId |}
                |> StoredEvent.EventDeleted
                |> success
        | LeaseCommand leaseCommand ->
            match leaseCommand with
            | CreateLease (effDate, newLease) ->
                let asOfDate = { AsAt = %DateTime.UtcNow; AsOn = effDate }
                let ctx = EventContext.create effDate streamState.NextEventId
                let leaseCreated = LeaseEvent.LeaseCreated (ctx, newLease)
                let effOrderOpt = leaseCreated |> LeaseEvent.getEventEffectiveOrder |> Some
                streamState 
                |> reconstitute asOfDate effOrderOpt
                |> Result.bind(function
                    | None -> leaseCreated |> LeaseEvent.toStoredEvent |> Ok
                    | Some _ -> Error "cannot create lease; lease already exists")
                |> Result.bimap success failure
            | SchedulePayment (effDate, paymentAmount) ->
                let asOfDate = { AsAt = %DateTime.UtcNow; AsOn = effDate }
                let ctx = EventContext.create effDate streamState.NextEventId
                let paymentScheduled = LeaseEvent.PaymentScheduled (ctx, paymentAmount)
                let effOrderOpt = paymentScheduled |> LeaseEvent.getEventEffectiveOrder |> Some
                streamState 
                |> reconstitute asOfDate effOrderOpt
                |> Result.bind(function
                    | None -> Error "cannot schedule payment; lease does not exist" 
                    | Some { LeaseStatus = leaseStatus } ->
                        match leaseStatus with
                        | Terminated -> Error "cannot schedule payment; lease is terminated"
                        | Outstanding -> paymentScheduled |> LeaseEvent.toStoredEvent |> Ok)
                |> Result.bimap success failure
            | ReceivePayment (effDate, paymentAmount) ->
                let asOfDate = { AsAt = %DateTime.UtcNow; AsOn = effDate }
                let ctx = EventContext.create effDate streamState.NextEventId
                let paymentReceived = LeaseEvent.PaymentReceived (ctx, paymentAmount)
                let effOrderOpt = paymentReceived |> LeaseEvent.getEventEffectiveOrder |> Some
                streamState 
                |> reconstitute asOfDate effOrderOpt
                |> Result.bind (function
                    | None -> Error "cannot receive payment; lease does not exist" 
                    | Some _ -> paymentReceived |> LeaseEvent.toStoredEvent |> Ok)
                |> Result.bimap success failure
            | TerminateLease effDate ->
                let asOfDate = { AsAt = %DateTime.UtcNow; AsOn = effDate }
                let ctx = EventContext.create effDate streamState.NextEventId
                let leaseTerminated = LeaseEvent.LeaseTerminated ctx
                let effOrderOpt = leaseTerminated |> LeaseEvent.getEventEffectiveOrder |> Some
                streamState 
                |> reconstitute asOfDate effOrderOpt
                |> Result.bind (function
                    | None -> Error "cannot terminate lease; lease does not exist" 
                    | Some { LeaseStatus = leaseStatus } ->
                        match leaseStatus with
                        | Terminated -> Error "cannot terminate lease; lease is already terminated"
                        | Outstanding -> leaseTerminated |> LeaseEvent.toStoredEvent |> Ok)
                |> Result.bimap success failure

let evolve
    : StreamState -> StoredEvent -> StreamState =
    fun streamState storedEvent ->
        let (|DeletedEvent|_|) = StoredEvent.tryToDeletedEvent
        let (|LeaseEvent|_|) = StoredEvent.tryToLeaseEvent
        match storedEvent with
        | DeletedEvent deletedEvent ->
            { streamState with
                DeletedEvents = deletedEvent :: streamState.DeletedEvents }
        | LeaseEvent leaseEvent ->
            { streamState with
                NextEventId = streamState.NextEventId + 1<eventId>
                LeaseEvents = leaseEvent :: streamState.LeaseEvents }
        | _ -> streamState

let fold
    : StreamState -> seq<StoredEvent> -> StreamState =
    Seq.fold evolve

let initialState =
    { NextEventId = 1<eventId>
      LeaseEvents = []
      DeletedEvents = [] }