module Lease.Aggregate

open FSharp.UMX
open System

module EventContext =
    let create 
        (getUtcNow: unit -> DateTimeOffset) =
        fun eventEffectiveDate eventEffectiveOrder eventType ->
            { EventCreatedAt = getUtcNow() |> UMX.tag<eventCreatedDate>
              EventEffectiveAt = eventEffectiveDate
              EventEffectiveOrder = eventEffectiveOrder
              EventType = eventType }

module LeaseEvent =
    let getEventType : LeaseEvent -> EventType =
        function
        | LeaseAccepted _ -> %"LeaseAccepted"
        | PaymentRequested _ -> %"PaymentRequested"
        | PaymentRejected _ -> %"PaymentRejected"
        | PaymentSettled _ -> %"PaymentSettled"
        | PaymentReturned _ -> %"PaymentReturned"
        | VehicleReturned _ -> %"VehicleReturned"
    let getEventEffectiveOrder : LeaseEvent -> EventEffectiveOrder =
        function
        | LeaseAccepted _ -> %0
        | PaymentRequested _ -> %1
        | PaymentRejected _ -> %2
        | PaymentSettled _ -> %3
        | PaymentReturned _ -> %4
        | VehicleReturned _ -> %5
    let getEventContext =
        function
        | LeaseAccepted p -> p.EventContext
        | PaymentRequested p -> p.EventContext
        | PaymentRejected p -> p.EventContext
        | PaymentSettled p -> p.EventContext
        | PaymentReturned p -> p.EventContext
        | VehicleReturned p -> p.EventContext
    let getSortOrder leaseEvent = 
        let ctx = getEventContext leaseEvent
        let order = getEventEffectiveOrder leaseEvent
        ctx.EventEffectiveAt, order, ctx.EventCreatedAt
    let asAtOrBefore 
        (asAt:EventCreatedDate) = 
        fun (event:LeaseEvent) ->
            let ctx = getEventContext event
            ctx.EventCreatedAt <= asAt
    let asOfOrBefore 
        (asOf:EventEffectiveDate)
        (orderOpt:EventEffectiveOrder option) =
        fun (event:LeaseEvent) ->
            let ctx = getEventContext event
            let eventEffOrder = getEventEffectiveOrder event
            match orderOpt with
            | Some order when ctx.EventEffectiveAt = asOf ->
                (eventEffOrder <= order)
            | _ ->
                (ctx.EventEffectiveAt <= asOf)

module LeaseState =
    let init (leaseId:LeaseId) =
        { LeaseId = leaseId
          EventContext = None
          AcceptedLease = None
          ReturnedVehicle = None
          Payments = Map.empty
          CumPaymentAmountScheduled = 0m<usd>
          CumPaymentAmountReceived = 0m<usd>
          CumPaymentAmountChargedOff = 0m<usd>
          DaysPastDue = 0<day>
          OutstandingPaymentAmount = 0m<usd>
          UnpaidPaymentAmount = 0m<usd> }

let evolveLeaseState : LeaseState -> LeaseEvent -> LeaseState =
    fun state event ->
        let ctx = event |> LeaseEvent.getEventContext
        match event with
        | Stored storedEvent ->
            match storedEvent with
            | LeaseAccepted payload ->
                { state with
                    EventContext = Some ctx
                    AcceptedLease = Some payload.AcceptedLease }
            | PaymentRequested payload ->
                let transactionId = payload.RequestedPayment.TransactionId
                let pmtState =
                    { PaymentStatus = Requested
                      PaymentAmount = payload.RequestedPayment.RequestedAmount
                      RequestedAt = payload.RequestedPayment.RequestedAt
                      RejectedAt = None
                      SettledAt = None
                      ReceivedAt = None
                      ReturnedAt = None }
                { state with
                    EventContext = Some ctx
                    Payments = state.Payments |> Map.add transactionId pmtState }
            | PaymentRejected payload ->
                let transactionId = payload.RejectedPayment.TransactionId
                let pmtState = state.Payments |> Map.find transactionId
                let newPmtState =
                    { pmtState with 
                        PaymentStatus = Rejected
                        RejectedAmount = payload.RejectedPayment.RejectedAmount }
                { state with
                    EventContext = Some ctx
                    Payments = state.Payments |> Map.add transactionId newPmtState }
            | PaymentSettled payload ->
                let transactionId = payload.SettledPayment.TransactionId
                let pmtState = state.Payments |> Map.find transactionId
            
module LeaseStream =
    // Get non-deleted events created at or before asAt.
    let getEffectiveLeaseEventsAsAt
        (asAt:EventCreatedTime) =
        fun { LeaseEvents = leaseEvents; DeletedEvents = deletedEvents } ->
            let deletedEventIds =
                deletedEvents
                |> List.choose (fun (deletedDate, deletedEventId) ->
                    if deletedDate <= asAt then Some deletedEventId
                    else None)
            leaseEvents
            |> List.filter (fun leaseEvent ->
                let { EventId = eventId } = leaseEvent |> LeaseEvent.getEventContext
                let notDeleted = not (deletedEventIds |> List.contains eventId)
                let atOrBeforeAsAt = leaseEvent |> LeaseEvent.asAtOrBefore asAt
                notDeleted && atOrBeforeAsAt)
    // Get non-deleted events created at or before asAt and effective on or before asOn.
    let getEffectiveLeaseEventsAsOf
        (asOf:AsOf)
        (effOrderOpt:EventEffectiveOrder option) =
        fun (leaseStream:LeaseStream) ->
            leaseStream
            |> getEffectiveLeaseEventsAsAt asOf.AsAt
            |> List.filter (LeaseEvent.asOnOrBefore asOf.AsOn effOrderOpt)

let reconstitute
    (leaseId:LeaseId)
    (asOf:AsOf)
    (effOrderOpt: EventEffectiveOrder option) 
    : LeaseStream -> LeaseState =
    fun streamState ->
        streamState
        |> LeaseStream.getEffectiveLeaseEventsAsOf asOf effOrderOpt
        |> List.sortBy LeaseEvent.getSortOrder
        |> List.fold (evolveLeaseState leaseId) None

let interpret
    (getUtcNow:unit -> DateTime)
    (leaseId:LeaseId)
    : Command -> LeaseStream -> StoredEvent list =
    fun command leaseStream ->
        let leaseIdStr = leaseId |> LeaseId.toStringN
        let getAsOf (effDate:EventEffectiveDate) =
            { AsAt = %getUtcNow(); AsOn = effDate }
        let createEventContext = 
            EventContext.create getUtcNow
        let reconstitute' asOf effOrderOpt =
            leaseStream |> reconstitute leaseId asOf effOrderOpt
        match command with
        | DeleteEvent eventId ->
            let alreadyDeleted =
                leaseStream.DeletedEvents
                |> List.exists (fun (_, deletedEventId) -> deletedEventId = eventId)
            if alreadyDeleted then
                sprintf "Lease-%s Event-%d already deleted" leaseIdStr eventId
                |> RpcException.raiseAlreadyExists
            else
                {| EventContext = {| EventCreatedTime = %getUtcNow() |}; EventId = eventId |}
                |> EventDeleted
                |> List.singleton
        | LeaseCommand leaseCommand ->
            match leaseCommand with
            | CreateLease lease ->
                let asOf = getAsOf %lease.CommencementDate
                let alreadyCreated =
                    leaseStream
                    |> LeaseStream.getEffectiveLeaseEventsAsAt asOf.AsAt 
                    |> List.exists (function LeaseEvent.LeaseCreated _ -> true | _ -> false)
                if alreadyCreated then
                    sprintf "Lease-%s already created" leaseIdStr
                    |> RpcException.raiseAlreadyExists
                let ctx = createEventContext %lease.CommencementDate leaseStream.NextEventId
                let leaseCreated = LeaseEvent.LeaseCreated (ctx, lease)
                let effOrderOpt = leaseCreated |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOf effOrderOpt with
                | None -> leaseCreated |> LeaseEvent.toStoredEvent |> List.singleton
                | Some _ -> 
                    sprintf "cannot create lease; Lease-%s already exists" leaseIdStr
                    |> RpcException.raiseInternal
            | SchedulePayment payment ->
                let asOf = getAsOf %payment.ScheduledDate
                let alreadyScheduled =
                    leaseStream
                    |> LeaseStream.getEffectiveLeaseEventsAsAt asOf.AsAt
                    |> List.exists (function 
                        | LeaseEvent.PaymentScheduled (_, p) -> p.PaymentId = payment.PaymentId 
                        | _ -> false)
                if alreadyScheduled then
                    sprintf "Lease-%s Payment-%s already scheduled" leaseIdStr (payment.PaymentId |> PaymentId.toStringN)
                    |> RpcException.raiseAlreadyExists
                let ctx = createEventContext %payment.ScheduledDate leaseStream.NextEventId
                let paymentScheduled = LeaseEvent.PaymentScheduled (ctx, payment)
                let effOrderOpt = paymentScheduled |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOf effOrderOpt with
                | None -> 
                    sprintf "cannot schedule payment; Lease-%s does not exist" leaseIdStr
                    |> RpcException.raiseInternal
                | Some { LeaseStatus = leaseStatus } ->
                    match leaseStatus with
                    | Terminated _ -> 
                        sprintf "cannot schedule payment; Lease-%s is terminated" leaseIdStr
                        |> RpcException.raiseInternal
                    | Outstanding -> paymentScheduled |> LeaseEvent.toStoredEvent |> List.singleton
            | ReceivePayment payment ->
                let asOf = getAsOf %payment.ReceivedDate
                let alreadyReceived =
                    leaseStream
                    |> LeaseStream.getEffectiveLeaseEventsAsAt asOf.AsAt
                    |> List.exists (function 
                        | LeaseEvent.PaymentReceived (_, p) -> p.PaymentId = payment.PaymentId
                        | _ -> false)
                if alreadyReceived then
                    sprintf "Lease-%s Payment-%s already received" leaseIdStr (payment.PaymentId |> PaymentId.toStringN)
                    |> RpcException.raiseAlreadyExists
                let ctx = createEventContext %payment.ReceivedDate leaseStream.NextEventId
                let paymentReceived = LeaseEvent.PaymentReceived (ctx, payment)
                let effOrderOpt = paymentReceived |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOf effOrderOpt with
                | None -> 
                    sprintf "cannot receive payment; Lease-%s does not exist" leaseIdStr
                    |> RpcException.raiseInternal
                | Some _ -> paymentReceived |> LeaseEvent.toStoredEvent |> List.singleton
            | TerminateLease termination ->
                let asOf = getAsOf %termination.TerminationDate
                let alreadyTerminated =
                    leaseStream
                    |> LeaseStream.getEffectiveLeaseEventsAsAt asOf.AsAt
                    |> List.exists (function 
                        | LeaseEvent.LeaseTerminated _ -> true 
                        | _ -> false)
                if alreadyTerminated then
                    sprintf "Lease-%s already terminated" leaseIdStr
                    |> RpcException.raiseAlreadyExists
                let ctx = createEventContext %termination.TerminationDate leaseStream.NextEventId
                let leaseTerminated = LeaseEvent.LeaseTerminated (ctx, termination)
                let effOrderOpt = leaseTerminated |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOf effOrderOpt with
                | None -> 
                    sprintf "cannot terminate lease; Lease-%s does not exist" leaseIdStr
                    |> RpcException.raiseInternal
                | Some { LeaseStatus = leaseStatus } ->
                    match leaseStatus with
                    | Terminated _ -> 
                        sprintf "cannot terminate lease; Lease-%s is already terminated" leaseIdStr
                        |> RpcException.raiseInternal
                    | Outstanding -> leaseTerminated |> LeaseEvent.toStoredEvent |> List.singleton

let evolveLeaseStream
    : LeaseStream -> StoredEvent -> LeaseStream =
    fun leaseStream storedEvent ->
        let (|DeletedEvent|_|) = StoredEvent.tryToDeletedEvent
        let (|LeaseEvent|_|) = StoredEvent.tryToLeaseEvent
        match storedEvent with
        | DeletedEvent deletedEvent ->
            { leaseStream with
                DeletedEvents = deletedEvent :: leaseStream.DeletedEvents }
        | LeaseEvent leaseEvent ->
            { leaseStream with
                NextEventId = leaseStream.NextEventId + 1<eventId>
                LeaseEvents = leaseEvent :: leaseStream.LeaseEvents }
        | _ -> leaseStream

let foldLeaseStream
    : LeaseStream -> seq<StoredEvent> -> LeaseStream =
    Seq.fold evolveLeaseStream

let initialLeaseStream =
    { NextEventId = 1<eventId>
      LeaseEvents = []
      DeletedEvents = [] }

let evolveLeaseEventList
    : LeaseEventList -> StoredEvent -> LeaseEventList =
    fun leaseEventList storedEvent ->
        let (|LeaseEvent|_|) = StoredEvent.tryToLeaseEvent
        let (|DeletedEvent|_|) = StoredEvent.tryToDeletedEvent
        match storedEvent with
        | LeaseEvent leaseEvent ->
            leaseEvent :: leaseEventList
        | DeletedEvent (_, deletedEventId) ->
            leaseEventList
            |> List.filter (fun leaseEvent -> 
                let ctx = leaseEvent |> LeaseEvent.getEventContext
                ctx.EventId <> deletedEventId)
        | _ -> leaseEventList

let foldLeaseEventList
    : LeaseEventList -> seq<StoredEvent> -> LeaseEventList =
    fun leaseEventList storedEvents ->
        storedEvents |> Seq.fold evolveLeaseEventList leaseEventList
