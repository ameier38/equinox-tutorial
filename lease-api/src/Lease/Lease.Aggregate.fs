module Lease.Aggregate

open FSharp.UMX
open Grpc.Core
open System

module EventContext =
    let create (getUtcNow: unit -> DateTime) =
        fun effDate eventId ->
            { EventId = eventId
              EventCreatedTime = getUtcNow() |> UMX.tag<eventCreatedTime>
              EventEffectiveDate = effDate }

module StoredEvent =
    let tryToDeletedEvent = function
        | EventDeleted payload ->
            Some (payload.EventContext.EventCreatedTime, payload.EventId)
        | _ -> None
    let tryToLeaseEvent = function
        | EventDeleted _ -> None
        | LeaseCreated payload ->
            (payload.EventContext, payload.Lease)
            |> LeaseEvent.LeaseCreated
            |> Some
        | PaymentScheduled payload ->
            (payload.EventContext, payload.ScheduledPayment)
            |> LeaseEvent.PaymentScheduled
            |> Some
        | PaymentReceived payload ->
            (payload.EventContext, payload.ReceivedPayment)
            |> LeaseEvent.PaymentReceived
            |> Some
        | LeaseTerminated payload ->
            (payload.EventContext, payload.Termination)
            |> LeaseEvent.LeaseTerminated
            |> Some

module LeaseEvent =
    let getEventType : LeaseEvent -> EventType =
        Union.toString >> UMX.tag<eventType>
    let getEventEffectiveOrder : LeaseEvent -> EventEffectiveOrder = function
        | LeaseEvent.LeaseCreated _ -> %1
        | LeaseEvent.PaymentScheduled _ -> %2
        | LeaseEvent.PaymentReceived _ -> %3
        | LeaseEvent.LeaseTerminated _ -> %4
    let getEventContext = function
        | LeaseEvent.LeaseCreated (ctx, _)
        | LeaseEvent.PaymentScheduled (ctx, _)
        | LeaseEvent.PaymentReceived (ctx, _)
        | LeaseEvent.LeaseTerminated (ctx, _) -> ctx
    let getOrder leaseEvent = 
        let ctx = getEventContext leaseEvent
        let effOrder = getEventEffectiveOrder leaseEvent
        ctx.EventEffectiveDate, effOrder, ctx.EventCreatedTime
    let asAtOrBefore 
        (asAt:EventCreatedTime) = 
        fun (event:LeaseEvent) ->
            let { EventCreatedTime = eventCreatedTime } = getEventContext event
            eventCreatedTime <= asAt
    let asOnOrBefore 
        (asOn:EventEffectiveDate)
        (effOrderOpt:EventEffectiveOrder option) =
        fun (event:LeaseEvent) ->
            let { EventEffectiveDate = eventEffectiveDate } = getEventContext event
            let eventEffectiveOrder = getEventEffectiveOrder event
            match effOrderOpt with
            | Some effOrder when eventEffectiveDate = asOn ->
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
        { Lease = lease
          CreatedTime = eventCreatedTime
          UpdatedTime = eventCreatedTime
          TotalScheduled = 0m<usd> 
          TotalPaid = 0m<usd> 
          AmountDue = 0m<usd> 
          LeaseStatus = Outstanding }
    let schedulePayment 
        (eventCreatedTime:EventCreatedTime)
        (payment:ScheduledPayment) =
        fun leaseObs ->
            let totalScheduled = leaseObs.TotalScheduled + payment.ScheduledAmount
            let amountDue = totalScheduled - leaseObs.TotalPaid
            { leaseObs with
                UpdatedTime = eventCreatedTime
                TotalScheduled = totalScheduled
                AmountDue = amountDue }
    let receivePayment 
        (eventCreatedTime:EventCreatedTime)
        (payment:ReceivedPayment) =
        fun leaseObs ->
            let totalPaid = leaseObs.TotalPaid + payment.ReceivedAmount
            let amountDue = leaseObs.TotalScheduled - totalPaid
            { leaseObs with
                UpdatedTime = eventCreatedTime
                TotalPaid = totalPaid
                AmountDue = amountDue }
    let terminateLease
        (eventCreatedTime:EventCreatedTime) 
        (termination:Termination)=
        fun leaseObs ->
            { leaseObs with
                UpdatedTime = eventCreatedTime
                LeaseStatus = Terminated }

let evolveLeaseState 
    (leaseId:LeaseId)
    : LeaseState -> LeaseEvent -> LeaseState =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    fun state -> function
        | LeaseEvent.LeaseCreated (ctx, lease) as event ->
            match state with
            | None -> LeaseObservation.createLease ctx.EventCreatedTime lease |> Some
            | Some _ -> 
                let msg = sprintf "cannot observe %A; Lease-%s already exists" event leaseIdStr
                RpcException(Status(StatusCode.Internal, msg))
                |> raise
        | LeaseEvent.PaymentScheduled (ctx, payment) as event ->
            match state with
            | None -> 
                let msg = sprintf "cannot observe %A; Lease-%s does not exist" event leaseIdStr
                RpcException(Status(StatusCode.Internal, msg))
                |> raise
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.schedulePayment ctx.EventCreatedTime payment 
                |> Some
        | LeaseEvent.PaymentReceived (ctx, payment) as event ->
            match state with
            | None -> 
                let msg = sprintf "cannot observe %A; Lease-%s does not exist" event leaseIdStr
                RpcException(Status(StatusCode.Internal, msg))
                |> raise
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.receivePayment ctx.EventCreatedTime payment 
                |> Some
        | LeaseEvent.LeaseTerminated (ctx, termination) as event ->
            match state with
            | None -> 
                let msg = sprintf "cannot observe %A; Lease-%s does not exist" event leaseIdStr
                RpcException(Status(StatusCode.Internal, msg))
                |> raise
            | Some leaseObs -> 
                leaseObs
                |> LeaseObservation.terminateLease ctx.EventCreatedTime termination
                |> Some
            
module LeaseStream =
    // Get non-deleted events created at before asAt.
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
                not (deletedEventIds |> List.contains eventId) && (leaseEvent |> LeaseEvent.asAtOrBefore asAt))
    // Get non-deleted events created at or before asAt and effective on or before asOn.
    let getEffectiveLeaseEventsAsOf
        (asOf:AsOfDate)
        (effOrderOpt:EventEffectiveOrder option) =
        fun (leaseStream:LeaseStream) ->
            leaseStream
            |> getEffectiveLeaseEventsAsAt asOf.AsAt
            |> List.filter (LeaseEvent.asOnOrBefore asOf.AsOn effOrderOpt)

let reconstitute
    (leaseId:LeaseId)
    (asOfDate:AsOfDate)
    (effOrderOpt: EventEffectiveOrder option) 
    : LeaseStream -> LeaseState =
    fun streamState ->
        streamState
        |> LeaseStream.getEffectiveLeaseEventsAsOf asOfDate effOrderOpt
        |> List.sortBy LeaseEvent.getOrder
        |> List.fold (evolveLeaseState leaseId) None

let interpret
    (getUtcNow:unit -> DateTime)
    (leaseId:LeaseId)
    : Command -> LeaseStream -> StoredEvent list =
    fun command leaseStream ->
        let leaseIdStr = leaseId |> LeaseId.toStringN
        let getAsOfDate (effDate:EventEffectiveDate) =
            { AsAt = %getUtcNow(); AsOn = effDate }
        let createEventContext = 
            EventContext.create getUtcNow
        let reconstitute' asOfDate effOrderOpt =
            leaseStream |> reconstitute leaseId asOfDate effOrderOpt
        match command with
        | DeleteEvent eventId ->
            let alreadyDeleted =
                leaseStream.DeletedEvents
                |> List.exists (fun (_, deletedEventId) -> deletedEventId = eventId)
            if alreadyDeleted then
                let msg = sprintf "Lease-%s Event-%d already deleted" leaseIdStr eventId
                RpcException(Status(StatusCode.AlreadyExists, msg))
                |> raise
            else
                {| EventContext = {| EventCreatedTime = %DateTime.UtcNow |}; EventId = eventId |}
                |> EventDeleted
                |> List.singleton
        | LeaseCommand leaseCommand ->
            match leaseCommand with
            | CreateLease lease ->
                let asOfDate = getAsOfDate %lease.StartDate
                let alreadyCreated =
                    leaseStream
                    |> LeaseStream.getEffectiveLeaseEventsAsAt asOfDate.AsAt 
                    |> List.exists (function LeaseEvent.LeaseCreated _ -> true | _ -> false)
                if alreadyCreated then
                    let msg = sprintf "Lease-%s already created" leaseIdStr
                    RpcException(Status(StatusCode.AlreadyExists, msg))
                    |> raise
                let ctx = createEventContext %lease.StartDate leaseStream.NextEventId
                let leaseCreated = LeaseEvent.LeaseCreated (ctx, lease)
                let effOrderOpt = leaseCreated |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> leaseCreated |> LeaseEvent.toStoredEvent |> List.singleton
                | Some _ -> 
                    let msg = sprintf "cannot create lease; Lease-%s already exists" leaseIdStr
                    RpcException(Status(StatusCode.Internal, msg))
                    |> raise
            | SchedulePayment payment ->
                let asOfDate = getAsOfDate %payment.ScheduledDate
                let alreadyScheduled =
                    leaseStream
                    |> LeaseStream.getEffectiveLeaseEventsAsAt asOfDate.AsAt
                    |> List.exists (function 
                        | LeaseEvent.PaymentScheduled (_, p) -> p.PaymentId = payment.PaymentId 
                        | _ -> false)
                if alreadyScheduled then
                    let msg = sprintf "Lease-%s Payment-%s already scheduled" leaseIdStr (payment.PaymentId |> PaymentId.toStringN)
                    RpcException(Status(StatusCode.AlreadyExists, msg))
                    |> raise
                let ctx = createEventContext %payment.ScheduledDate leaseStream.NextEventId
                let paymentScheduled = LeaseEvent.PaymentScheduled (ctx, payment)
                let effOrderOpt = paymentScheduled |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> 
                    let msg = sprintf "cannot schedule payment; Lease-%s does not exist" leaseIdStr
                    RpcException(Status(StatusCode.Internal, msg))
                    |> raise
                | Some { LeaseStatus = leaseStatus } ->
                    match leaseStatus with
                    | Terminated -> 
                        let msg = sprintf "cannot schedule payment; Lease-%s is terminated" leaseIdStr
                        RpcException(Status(StatusCode.Internal, msg))
                        |> raise
                    | Outstanding -> paymentScheduled |> LeaseEvent.toStoredEvent |> List.singleton
            | ReceivePayment payment ->
                let asOfDate = getAsOfDate %payment.ReceivedDate
                let alreadyReceived =
                    leaseStream
                    |> LeaseStream.getEffectiveLeaseEventsAsAt asOfDate.AsAt
                    |> List.exists (function 
                        | LeaseEvent.PaymentReceived (_, p) -> p.PaymentId = payment.PaymentId
                        | _ -> false)
                if alreadyReceived then
                    let msg = sprintf "Lease-%s Payment-%s already received" leaseIdStr (payment.PaymentId |> PaymentId.toStringN)
                    RpcException(Status(StatusCode.AlreadyExists, msg))
                    |> raise
                let ctx = createEventContext %payment.ReceivedDate leaseStream.NextEventId
                let paymentReceived = LeaseEvent.PaymentReceived (ctx, payment)
                let effOrderOpt = paymentReceived |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> 
                    let msg = sprintf "cannot receive payment; Lease-%s does not exist" leaseIdStr
                    RpcException(Status(StatusCode.Internal, msg))
                    |> raise
                | Some _ -> paymentReceived |> LeaseEvent.toStoredEvent |> List.singleton
            | TerminateLease termination ->
                let asOfDate = getAsOfDate %termination.TerminationDate
                let alreadyTerminated =
                    leaseStream
                    |> LeaseStream.getEffectiveLeaseEventsAsAt asOfDate.AsAt
                    |> List.exists (function 
                        | LeaseEvent.LeaseTerminated _ -> true 
                        | _ -> false)
                if alreadyTerminated then
                    let msg = sprintf "Lease-%s already terminated" leaseIdStr
                    RpcException(Status(StatusCode.AlreadyExists, msg))
                    |> raise
                let ctx = createEventContext %termination.TerminationDate leaseStream.NextEventId
                let leaseTerminated = LeaseEvent.LeaseTerminated (ctx, termination)
                let effOrderOpt = leaseTerminated |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> 
                    let msg = sprintf "cannot terminate lease; Lease-%s does not exist" leaseIdStr
                    RpcException(Status(StatusCode.Internal, msg))
                    |> raise
                | Some { LeaseStatus = leaseStatus } ->
                    match leaseStatus with
                    | Terminated -> 
                        let msg = sprintf "cannot terminate lease; Lease-%s is already terminated" leaseIdStr
                        RpcException(Status(StatusCode.Internal, msg))
                        |> raise
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

let evolveLeaseCreatedList
    : LeaseCreatedList -> StoredEvent -> LeaseCreatedList =
    fun leaseCreatedList storedEvent ->
        let (|LeaseEvent|_|) = StoredEvent.tryToLeaseEvent
        let (|DeletedEvent|_|) = StoredEvent.tryToDeletedEvent
        match storedEvent with
        | LeaseEvent leaseEvent ->
            match leaseEvent with
            | LeaseEvent.LeaseCreated (ctx, lease) ->
                (ctx, lease) :: leaseCreatedList
            | _ -> leaseCreatedList
        | DeletedEvent (_, deletedEventId) ->
            leaseCreatedList
            |> List.filter (fun (ctx, _) -> ctx.EventId <> deletedEventId)
        | _ -> leaseCreatedList

let foldLeaseList
    : LeaseCreatedList -> seq<StoredEvent> -> LeaseCreatedList =
    fun leaseList storedEvents ->
        storedEvents |> Seq.fold evolveLeaseCreatedList leaseList

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
