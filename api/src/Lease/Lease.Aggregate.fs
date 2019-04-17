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
        | StoredEvent.LeaseUpdated payload ->
            (payload.EventContext, payload.UpdatedLease)
            |> LeaseEvent.LeaseUpdated
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
    let getEventEffectiveOrder : LeaseEvent -> EventEffectiveOrder = function
        | LeaseEvent.LeaseCreated _ -> %1
        | LeaseEvent.LeaseUpdated _ -> %2
        | LeaseEvent.PaymentScheduled _ -> %3
        | LeaseEvent.PaymentReceived _ -> %4
        | LeaseEvent.LeaseTerminated _ -> %5
    let getEventContext = function
        | LeaseEvent.LeaseCreated (ctx, _)
        | LeaseEvent.LeaseUpdated (ctx, _)
        | LeaseEvent.PaymentScheduled (ctx, _)
        | LeaseEvent.PaymentReceived (ctx, _)
        | LeaseEvent.LeaseTerminated ctx -> ctx
    let getOrder leaseEvent = 
        let ctx = getEventContext leaseEvent
        let effOrder = getEventEffectiveOrder leaseEvent
        ctx.EventEffectiveDate, effOrder, ctx.EventCreatedDate
    let asOfOrBefore 
        (AsOfDate (effDate, createdDate):AsOfDate)
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
        | LeaseEvent.LeaseUpdated (ctx, updatedLease) ->
            {| EventContext = ctx; UpdatedLease = updatedLease |}
            |> StoredEvent.LeaseUpdated
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
    let updateLease 
        (updatedLease:UpdatedLease) =
        fun leaseObs ->
            let maturityDate = 
                updatedLease.MaturityDate 
                |> Option.defaultValue leaseObs.MaturityDate
            let monthlyPaymentAmount =
                updatedLease.MonthlyPaymentAmount
                |> Option.defaultValue leaseObs.MonthlyPaymentAmount
            { leaseObs with
                MaturityDate = maturityDate
                MonthlyPaymentAmount = monthlyPaymentAmount }
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
            match state with
            | Corrupt err -> Corrupt err
            | Nonexistent -> LeaseObservation.createLease effDate newLease |> Exists
            | _ -> Corrupt "cannot observe LeaseCreated; lease already exists"
        | LeaseEvent.LeaseUpdated (_, updatedLease) ->
            match state with
            | Corrupt err -> Corrupt err
            | Nonexistent -> Corrupt "cannot observe LeaseUpdated; lease does not exist"
            | Exists leaseObs -> leaseObs |> LeaseObservation.updateLease updatedLease |> Exists
        | LeaseEvent.PaymentScheduled (_, paymentAmount) ->
            match state with
            | Corrupt err -> Corrupt err
            | Nonexistent -> Corrupt "cannot observe PaymentScheduled; lease does not exist"
            | Exists leaseObs -> leaseObs |> LeaseObservation.schedulePayment paymentAmount |> Exists
        | LeaseEvent.PaymentReceived (_, paymentAmount) ->
            match state with
            | Corrupt err -> Corrupt err
            | Nonexistent -> Corrupt "cannot observe PaymentReceived; lease does not exist"
            | Exists leaseObs -> leaseObs |> LeaseObservation.receivePayment paymentAmount |> Exists
        | LeaseEvent.LeaseTerminated _ ->
            match state with
            | Corrupt err -> Corrupt err
            | Nonexistent -> Corrupt "cannot observe LeaseTerminated"
            | Exists leaseObs -> leaseObs |> LeaseObservation.terminateLease |> Exists

let reconstitute
    (AsOfDate (_, createdDate) as asOfDate)
    (effOrderOpt: EventEffectiveOrder option) =
    fun { LeaseEvents = leaseEvents; DeletedEvents = deletedEvents} ->
        let deletedEventIds =
            deletedEvents
            |> List.choose (fun (deletedDate, deletedEventId) ->
                if deletedDate <= createdDate then Some deletedEventId
                else None)
        let isEffective leaseEvent = 
            let ctx = leaseEvent |> LeaseEvent.getEventContext
            let inRange = leaseEvent |> LeaseEvent.asOfOrBefore asOfDate effOrderOpt
            let notDeleted = deletedEventIds |> List.contains ctx.EventId |> not
            inRange && notDeleted
        leaseEvents
        |> List.filter isEffective
        |> List.sortBy LeaseEvent.getOrder
        |> List.fold observe Nonexistent

let decide
    (command:LeaseCommand)
    : StreamState -> Result<unit,string> * StoredEvent list =
    let ok = Ok ()
    let success (storedEvent:StoredEvent) = (ok, [storedEvent])
    let failure msg = (Error msg, [])
    fun streamState ->
        match command with
        | CreateLease (effDate, newLease) ->
            let asOfDate = AsOfDate (effDate, %DateTime.UtcNow)
            let ctx = EventContext.create effDate streamState.NextEventId
            let leaseCreated = LeaseEvent.LeaseCreated (ctx, newLease)
            let effOrderOpt = leaseCreated |> LeaseEvent.getEventEffectiveOrder |> Some
            match streamState |> reconstitute asOfDate effOrderOpt with
            | Corrupt err -> failure err
            | Nonexistent -> leaseCreated |> LeaseEvent.toStoredEvent |> success
            | _ -> failure "cannot create lease; lease already exists"
        | UpdateLease (effDate, updatedLease) ->
            let asOfDate = AsOfDate (effDate, %DateTime.UtcNow)
            let ctx = EventContext.create effDate streamState.NextEventId
            let leaseUpdated = LeaseEvent.LeaseUpdated (ctx, updatedLease)
            let effOrderOpt = leaseUpdated |> LeaseEvent.getEventEffectiveOrder |> Some
            match streamState |> reconstitute asOfDate effOrderOpt with
            | Corrupt err -> failure err
            | Nonexistent -> failure "cannot update lease; lease does not exist"
            | Exists _ -> leaseUpdated |> LeaseEvent.toStoredEvent |> success 
        | SchedulePayment (effDate, paymentAmount) ->
            let asOfDate = AsOfDate (effDate, %DateTime.UtcNow)
            let ctx = EventContext.create effDate streamState.NextEventId
            let paymentScheduled = LeaseEvent.PaymentScheduled (ctx, paymentAmount)
            let effOrderOpt = paymentScheduled |> LeaseEvent.getEventEffectiveOrder |> Some
            match streamState |> reconstitute asOfDate effOrderOpt with
            | Corrupt err -> failure err
            | Nonexistent -> failure "cannot schedule payment; lease does not exist" 
            | Exists { LeaseStatus = leaseStatus } ->
                match leaseStatus with
                | Terminated -> failure "cannot schedule payment; lease is terminated"
                | Outstanding -> paymentScheduled |> LeaseEvent.toStoredEvent |> success
        | ReceivePayment (effDate, paymentAmount) ->
            let asOfDate = AsOfDate (effDate, %DateTime.UtcNow)
            let ctx = EventContext.create effDate streamState.NextEventId
            let paymentReceived = LeaseEvent.PaymentReceived (ctx, paymentAmount)
            let effOrderOpt = paymentReceived |> LeaseEvent.getEventEffectiveOrder |> Some
            match streamState |> reconstitute asOfDate effOrderOpt with
            | Corrupt err -> failure err
            | Nonexistent -> failure "cannot receive payment; lease does not exist" 
            | Exists _ -> paymentReceived |> LeaseEvent.toStoredEvent |> success
        | TerminateLease effDate ->
            let asOfDate = AsOfDate (effDate, %DateTime.UtcNow)
            let ctx = EventContext.create effDate streamState.NextEventId
            let leaseTerminated = LeaseEvent.LeaseTerminated ctx
            let effOrderOpt = leaseTerminated |> LeaseEvent.getEventEffectiveOrder |> Some
            match streamState |> reconstitute asOfDate effOrderOpt with
            | Corrupt err -> failure err
            | Nonexistent -> failure "cannot terminate lease; lease does not exist" 
            | Exists { LeaseStatus = leaseStatus } ->
                match leaseStatus with
                | Terminated -> failure "cannot terminate lease; lease is already terminated"
                | Outstanding -> leaseTerminated |> LeaseEvent.toStoredEvent |> success

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
