module Lease.Aggregate

open FSharp.UMX
open System

module EventContext =
    let create (getUtcNow: unit -> DateTimeOffset) =
        fun effDate ->
            { CreatedAt = getUtcNow() |> UMX.tag<createdDate>
              EffectiveAt = effDate }

module LeaseEvent =
    let getEventType : LeaseEvent -> EventType =
        function
        | LeaseAccepted _ -> %"LeaseAccepted"
        | PaymentRequested _ -> %"PaymentRequested"
        | PaymentRejected _ -> %"PaymentRejected"
        | PaymentSettled _ -> %"PaymentSettled"
        | PaymentReturned _ -> %"PaymentReturned"
        | VehicleReturned _ -> %"VehicleReturned"
    let getEventOrder : LeaseEvent -> EventOrder =
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
        let order = getEventOrder leaseEvent
        ctx.EffectiveAt, order, ctx.CreatedAt
    let asAtOrBefore 
        (asAt:CreatedDate) = 
        fun (event:LeaseEvent) ->
            let ctx = getEventContext event
            ctx.CreatedAt <= asAt
    let asOfOrBefore 
        (asOf:EffectiveDate)
        (eventOrderOpt:EventOrder option) =
        fun (event:LeaseEvent) ->
            let ctx = getEventContext event
            let eventOrder = getEventOrder event
            match eventOrderOpt with
            | Some effOrder when ctx.EffectiveAt = asOn ->
                (eventEffectiveOrder <= effOrder)
            | _ ->
                (eventEffectiveDate <= asOn)
    let toStoredEvent = function
        | LeaseEvent.LeaseCreated (ctx, lease) ->
            {| EventContext = ctx; Lease = lease |}
            |> LeaseCreated
        | LeaseEvent.PaymentScheduled (ctx, payment) ->
            {| EventContext = ctx; ScheduledPayment = payment |}
            |> PaymentScheduled
        | LeaseEvent.PaymentReceived (ctx, payment) ->
            {| EventContext = ctx; ReceivedPayment = payment |}
            |> PaymentReceived
        | LeaseEvent.LeaseTerminated (ctx, termination) ->
            {| EventContext = ctx; Termination = termination |}
            |> LeaseTerminated

module LeaseObservation =
    let createLease 
        (eventCreatedTime:EventCreatedTime)
        (lease:Lease) =
        { CreatedAt = eventCreatedTime
          UpdatedAt = eventCreatedTime
          UpdatedOn = %lease.CommencementDate
          LeaseId = lease.LeaseId
          UserId = lease.UserId
          CommencementDate = lease.CommencementDate
          ExpirationDate = lease.ExpirationDate
          MonthlyPaymentAmount = lease.MonthlyPaymentAmount
          TotalScheduled = 0m<usd> 
          TotalPaid = 0m<usd> 
          AmountDue = 0m<usd> 
          LeaseStatus = Outstanding
          TerminatedDate = None }
    let schedulePayment 
        (eventCreatedTime:EventCreatedTime)
        (payment:ScheduledPayment) =
        fun leaseObs ->
            let totalScheduled = leaseObs.TotalScheduled + payment.ScheduledAmount
            let amountDue = totalScheduled - leaseObs.TotalPaid
            { leaseObs with
                UpdatedAt = eventCreatedTime
                UpdatedOn = %payment.ScheduledDate
                TotalScheduled = totalScheduled
                AmountDue = amountDue }
    let receivePayment 
        (eventCreatedTime:EventCreatedTime)
        (payment:ReceivedPayment) =
        fun leaseObs ->
            let totalPaid = leaseObs.TotalPaid + payment.ReceivedAmount
            let amountDue = leaseObs.TotalScheduled - totalPaid
            { leaseObs with
                UpdatedAt = eventCreatedTime
                UpdatedOn = %payment.ReceivedDate
                TotalPaid = totalPaid
                AmountDue = amountDue }
    let terminateLease
        (eventCreatedTime:EventCreatedTime)
        (termination: Termination) =
        fun leaseObs ->
            { leaseObs with
                UpdatedAt = eventCreatedTime
                UpdatedOn = %termination.TerminationDate
                LeaseStatus = Terminated
                TerminatedDate = Some termination.TerminationDate }

let evolveLeaseState 
    (leaseId:LeaseId)
    : LeaseState -> LeaseEvent -> LeaseState =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    fun state -> function
        | LeaseEvent.LeaseCreated (ctx, lease) as event ->
            match state with
            | None -> LeaseObservation.createLease ctx.EventCreatedTime lease |> Some
            | Some _ -> 
                sprintf "cannot observe %A; Lease-%s already exists" event leaseIdStr
                |> RpcException.raiseInternal
        | LeaseEvent.PaymentScheduled (ctx, payment) as event ->
            match state with
            | None -> 
                sprintf "cannot observe %A; Lease-%s does not exist" event leaseIdStr
                |> RpcException.raiseInternal
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.schedulePayment ctx.EventCreatedTime payment 
                |> Some
        | LeaseEvent.PaymentReceived (ctx, payment) as event ->
            match state with
            | None -> 
                sprintf "cannot observe %A; Lease-%s does not exist" event leaseIdStr
                |> RpcException.raiseInternal
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.receivePayment ctx.EventCreatedTime payment 
                |> Some
        | LeaseEvent.LeaseTerminated (ctx, termination) as event ->
            match state with
            | None -> 
                sprintf "cannot observe %A; Lease-%s does not exist" event leaseIdStr
                |> RpcException.raiseInternal
            | Some leaseObs -> 
                leaseObs
                |> LeaseObservation.terminateLease ctx.EventCreatedTime termination
                |> Some
            
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
