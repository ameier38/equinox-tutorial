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
            Some (payload.EventContext.EventCreatedDate, payload.EventId)
        | _ -> None
    let tryToLeaseEvent = function
        | EventDeleted _ -> None
        | LeaseCreated payload ->
            (payload.EventContext, payload.Lease)
            |> LeaseEvent.LeaseCreated
            |> Some
        | PaymentScheduled payload ->
            (payload.EventContext, payload.Payment)
            |> LeaseEvent.PaymentScheduled
            |> Some
        | PaymentReceived payload ->
            (payload.EventContext, payload.Payment)
            |> LeaseEvent.PaymentReceived
            |> Some
        | LeaseTerminated payload ->
            payload.EventContext
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
        | LeaseEvent.LeaseTerminated ctx -> ctx
    let getOrder leaseEvent = 
        let ctx = getEventContext leaseEvent
        let effOrder = getEventEffectiveOrder leaseEvent
        ctx.EventEffectiveDate, effOrder, ctx.EventCreatedTime
    let asOfOrBefore 
        ({ AsAt = createdTime; AsOn = effDate })
        (effOrderOpt:EventEffectiveOrder option) =
        fun (event:LeaseEvent) ->
            let { EventCreatedTime = eventCreatedTime
                  EventEffectiveDate = eventEffectiveDate } = getEventContext event
            let eventEffectiveOrder = getEventEffectiveOrder event
            match effOrderOpt with
            | Some effOrder when eventEffectiveDate = effDate ->
                (eventEffectiveOrder <= effOrder) && (eventCreatedTime <= createdTime)
            | _ ->
                (eventEffectiveDate <= effDate) && (eventCreatedTime <= createdTime)
    let toStoredEvent = function
        | LeaseEvent.LeaseCreated (ctx, lease) ->
            {| EventContext = ctx; Lease = lease |}
            |> LeaseCreated
        | LeaseEvent.PaymentScheduled (ctx, payment) ->
            {| EventContext = ctx; Payment = payment |}
            |> PaymentScheduled
        | LeaseEvent.PaymentReceived (ctx, payment) ->
            {| EventContext = ctx; Payment = payment |}
            |> PaymentReceived
        | LeaseEvent.LeaseTerminated ctx ->
            {| EventContext = ctx |}
            |> LeaseTerminated

module LeaseObservation =
    let createLease 
        (eventCreatedTime:EventCreatedTime)
        (lease:Lease) =
        { 
            Lease = lease 
            CreatedTime = eventCreatedTime
            UpdatedTime = eventCreatedTime
            TotalScheduled = 0m<usd> 
            TotalPaid = 0m<usd> 
            AmountDue = 0m<usd> 
            LeaseStatus = Outstanding 
        }
    let schedulePayment 
        (eventCreatedTime:EventCreatedTime)
        (payment:Payment) =
        fun leaseObs ->
            let totalScheduled = leaseObs.TotalScheduled + payment.PaymentAmount
            let amountDue = totalScheduled - leaseObs.TotalPaid
            { leaseObs with
                UpdatedTime = eventCreatedTime
                TotalScheduled = totalScheduled
                AmountDue = amountDue }
    let receivePayment 
        (eventCreatedTime:EventCreatedTime)
        (payment:Payment) =
        fun leaseObs ->
            let totalPaid = leaseObs.TotalPaid + payment.PaymentAmount
            let amountDue = leaseObs.TotalScheduled - totalPaid
            { leaseObs with
                UpdatedTime = eventCreatedTime
                TotalPaid = totalPaid
                AmountDue = amountDue }
    let terminateLease
        (eventCreatedTime:EventCreatedTime) =
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
                let msg = sprintf "cannot observe %A; lease-%s already exists" event leaseIdStr
                RpcException(Status(StatusCode.Internal, msg))
                |> raise
        | LeaseEvent.PaymentScheduled (ctx, payment) as event ->
            match state with
            | None -> 
                let msg = sprintf "cannot observe %A; lease-%s does not exist" event leaseIdStr
                RpcException(Status(StatusCode.Internal, msg))
                |> raise
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.schedulePayment ctx.EventCreatedTime payment 
                |> Some
        | LeaseEvent.PaymentReceived (ctx, payment) as event ->
            match state with
            | None -> 
                let msg = sprintf "cannot observe %A; lease-%s does not exist" event leaseIdStr
                RpcException(Status(StatusCode.Internal, msg))
                |> raise
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.receivePayment ctx.EventCreatedTime payment 
                |> Some
        | LeaseEvent.LeaseTerminated ctx as event ->
            match state with
            | None -> 
                let msg = sprintf "cannot observe %A; lease-%s does not exist" event leaseIdStr
                RpcException(Status(StatusCode.Internal, msg))
                |> raise
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.terminateLease ctx.EventCreatedTime 
                |> Some
            
module LeaseStream =
    let getLeaseEvents
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
                not (deletedEventIds |> List.contains eventId))
    let getEffectiveLeaseEvents
        (asOfDate:AsOfDate)
        (effOrderOpt:EventEffectiveOrder option) =
        fun (leaseStream:LeaseStream) ->
            leaseStream
            |> getLeaseEvents asOfDate.AsAt
            |> List.filter (LeaseEvent.asOfOrBefore asOfDate effOrderOpt)

let reconstitute
    (leaseId:LeaseId)
    (asOfDate:AsOfDate)
    (effOrderOpt: EventEffectiveOrder option) 
    : LeaseStream -> LeaseState =
    fun streamState ->
        streamState
        |> LeaseStream.getEffectiveLeaseEvents asOfDate effOrderOpt
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
                let msg = sprintf "lease-%s event-%d already deleted" leaseIdStr eventId
                RpcException(Status(StatusCode.AlreadyExists, msg))
                |> raise
            else
                {| EventContext = {| EventCreatedDate = %DateTime.UtcNow |}; EventId = eventId |}
                |> EventDeleted
                |> List.singleton
        | LeaseCommand leaseCommand ->
            match leaseCommand with
            | CreateLease (effDate, newLease) ->
                let asOfDate = getAsOfDate effDate
                let alreadyCreated =
                    leaseStream
                    |> LeaseStream.getLeaseEvents asOfDate.AsAt 
                    |> List.exists (function LeaseEvent.LeaseCreated _ -> true | _ -> false)
                if alreadyCreated then
                    let msg = sprintf "lease-%s already created" leaseIdStr
                    RpcException(Status(StatusCode.AlreadyExists, msg))
                    |> raise
                let ctx = createEventContext effDate leaseStream.NextEventId
                let leaseCreated = LeaseEvent.LeaseCreated (ctx, newLease)
                let effOrderOpt = leaseCreated |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> leaseCreated |> LeaseEvent.toStoredEvent |> List.singleton
                | Some _ -> 
                    let msg = sprintf "cannot create lease; lease-%s already exists" leaseIdStr
                    RpcException(Status(StatusCode.Internal, msg))
                    |> raise
            | SchedulePayment (effDate, payment) ->
                let asOfDate = getAsOfDate effDate
                let alreadyScheduled =
                    leaseStream
                    |> LeaseStream.getLeaseEvents asOfDate.AsAt
                    |> List.exists (function 
                        | LeaseEvent.PaymentScheduled (_, p) -> p.PaymentId = payment.PaymentId 
                        | _ -> false)
                if alreadyScheduled then
                    let msg = sprintf "lease-%s payment-%s already scheduled" leaseIdStr (payment.PaymentId |> PaymentId.toStringN)
                    RpcException(Status(StatusCode.AlreadyExists, msg))
                    |> raise
                let ctx = createEventContext effDate leaseStream.NextEventId
                let paymentScheduled = LeaseEvent.PaymentScheduled (ctx, payment)
                let effOrderOpt = paymentScheduled |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> 
                    let msg = sprintf "cannot schedule payment; lease-%s does not exist" leaseIdStr
                    RpcException(Status(StatusCode.Internal, msg))
                    |> raise
                | Some { LeaseStatus = leaseStatus } ->
                    match leaseStatus with
                    | Terminated -> 
                        let msg = sprintf "cannot schedule payment; lease-%s is terminated" leaseIdStr
                        RpcException(Status(StatusCode.Internal, msg))
                        |> raise
                    | Outstanding -> paymentScheduled |> LeaseEvent.toStoredEvent |> List.singleton
            | ReceivePayment (effDate, payment) ->
                let asOfDate = getAsOfDate effDate
                let alreadyReceived =
                    leaseStream
                    |> LeaseStream.getLeaseEvents asOfDate.AsAt
                    |> List.exists (function 
                        | LeaseEvent.PaymentReceived (_, p) -> p.PaymentId = payment.PaymentId
                        | _ -> false)
                if alreadyReceived then
                    let msg = sprintf "lease-%s payment-%s already received" leaseIdStr (payment.PaymentId |> PaymentId.toStringN)
                    RpcException(Status(StatusCode.AlreadyExists, msg))
                    |> raise
                let ctx = createEventContext effDate leaseStream.NextEventId
                let paymentReceived = LeaseEvent.PaymentReceived (ctx, payment)
                let effOrderOpt = paymentReceived |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> 
                    let msg = sprintf "cannot receive payment; lease-%s does not exist" leaseIdStr
                    RpcException(Status(StatusCode.Internal, msg))
                    |> raise
                | Some _ -> paymentReceived |> LeaseEvent.toStoredEvent |> List.singleton
            | TerminateLease effDate ->
                let asOfDate = getAsOfDate effDate
                let alreadyTerminated =
                    leaseStream
                    |> LeaseStream.getLeaseEvents asOfDate.AsAt
                    |> List.exists (function 
                        | LeaseEvent.LeaseTerminated _ -> true 
                        | _ -> false)
                if alreadyTerminated then
                    let msg = sprintf "lease-%s already terminated" leaseIdStr
                    RpcException(Status(StatusCode.AlreadyExists, msg))
                    |> raise
                let ctx = createEventContext effDate leaseStream.NextEventId
                let leaseTerminated = LeaseEvent.LeaseTerminated ctx
                let effOrderOpt = leaseTerminated |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> 
                    let msg = sprintf "cannot terminate lease; lease-%s does not exist" leaseIdStr
                    RpcException(Status(StatusCode.Internal, msg))
                    |> raise
                | Some { LeaseStatus = leaseStatus } ->
                    match leaseStatus with
                    | Terminated -> 
                        let msg = sprintf "cannot terminate lease; lease-%s is already terminated" leaseIdStr
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

let evolveLeaseList
    : LeaseList -> StoredEvent -> LeaseList =
    fun leaseList storedEvent ->
        let (|LeaseEvent|_|) = StoredEvent.tryToLeaseEvent
        let (|DeletedEvent|_|) = StoredEvent.tryToDeletedEvent
        match storedEvent with
        | LeaseEvent leaseEvent ->
            match leaseEvent with
            | LeaseEvent.LeaseCreated (ctx, lease) ->
                (ctx, lease) :: leaseList
            | _ -> leaseList
        | DeletedEvent (_, deletedEventId) ->
            leaseList
            |> List.filter (fun (ctx, _) -> ctx.EventId <> deletedEventId)
        | _ -> leaseList

let foldLeaseList
    : LeaseList -> seq<StoredEvent> -> LeaseList =
    fun leaseList storedEvents ->
        storedEvents |> Seq.fold evolveLeaseList leaseList

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