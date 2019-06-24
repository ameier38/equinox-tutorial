module Lease.Aggregate

open FSharp.UMX
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
            (payload.EventContext, payload.NewLease)
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
        ({ AsAt = createdDate; AsOn = effDate })
        (effOrderOpt:EventEffectiveOrder option) =
        fun (event:LeaseEvent) ->
            let { EventCreatedTime = eventCreatedDate
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
        (payment:Payment) =
        fun leaseObs ->
            let totalScheduled = leaseObs.TotalScheduled + payment.PaymentAmount
            let amountDue = totalScheduled - leaseObs.TotalPaid
            { leaseObs with
                TotalScheduled = totalScheduled
                AmountDue = amountDue }
    let receivePayment 
        (payment:Payment) =
        fun leaseObs ->
            let totalPaid = leaseObs.TotalPaid + payment.PaymentAmount
            let amountDue = leaseObs.TotalScheduled - totalPaid
            { leaseObs with
                TotalPaid = totalPaid
                AmountDue = amountDue }
    let terminateLease =
        fun leaseObs ->
            { leaseObs with
                LeaseStatus = Terminated }

let evolveLeaseState 
    (leaseId:LeaseId)
    : LeaseState -> LeaseEvent -> LeaseState =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    fun state -> function
        | LeaseEvent.LeaseCreated ({ EventEffectiveDate = effDate }, newLease) as event ->
            match state with
            | None -> LeaseObservation.createLease effDate newLease |> Some
            | Some _ -> failwithf "cannot observe %A; lease-%s already exists" event leaseIdStr
        | LeaseEvent.PaymentScheduled (_, payment) as event ->
            match state with
            | None -> failwithf "cannot observe %A; lease-%s does not exist" event leaseIdStr
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.schedulePayment payment 
                |> Some
        | LeaseEvent.PaymentReceived (_, payment) as event ->
            match state with
            | None -> failwithf "cannot observe %A; lease-%s does not exist" event leaseIdStr
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.receivePayment payment 
                |> Some
        | LeaseEvent.LeaseTerminated _ as event ->
            match state with
            | None -> failwithf "cannot observe %A; lease-%s does not exist" event leaseIdStr
            | Some leaseObs -> 
                leaseObs 
                |> LeaseObservation.terminateLease 
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
            let deletedEventIds =
                leaseStream.DeletedEvents
                |> List.map (fun (_, eventId) -> eventId)
            if deletedEventIds |> List.contains eventId then
                sprintf "lease-%s event-%d already deleted" leaseIdStr eventId
                |> DuplicateCommandException
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
                    sprintf "lease-%s already created" leaseIdStr
                    |> DuplicateCommandException
                    |> raise
                let ctx = createEventContext effDate leaseStream.NextEventId
                let leaseCreated = LeaseEvent.LeaseCreated (ctx, newLease)
                let effOrderOpt = leaseCreated |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> leaseCreated |> LeaseEvent.toStoredEvent |> List.singleton
                | Some _ -> failwithf "cannot create lease; lease-%s already exists" leaseIdStr
            | SchedulePayment (effDate, payment) ->
                let asOfDate = getAsOfDate effDate
                let alreadyScheduled =
                    leaseStream
                    |> LeaseStream.getLeaseEvents asOfDate.AsAt
                    |> List.choose (function LeaseEvent.PaymentScheduled (_, p) -> Some p.PaymentId | _ -> None)
                    |> List.exists (fun pid -> pid = payment.PaymentId)
                if alreadyScheduled then
                    sprintf "lease-%s payment-%s already scheduled" leaseIdStr (payment.PaymentId |> PaymentId.toStringN)
                    |> DuplicateCommandException
                    |> raise
                let ctx = createEventContext effDate leaseStream.NextEventId
                let paymentScheduled = LeaseEvent.PaymentScheduled (ctx, payment)
                let effOrderOpt = paymentScheduled |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> failwithf "cannot schedule payment; lease-%s does not exist" leaseIdStr
                | Some { LeaseStatus = leaseStatus } ->
                    match leaseStatus with
                    | Terminated -> failwithf "cannot schedule payment; lease-%s is terminated" leaseIdStr
                    | Outstanding -> paymentScheduled |> LeaseEvent.toStoredEvent |> List.singleton
            | ReceivePayment (effDate, payment) ->
                let asOfDate = getAsOfDate effDate
                let alreadyReceived =
                    leaseStream
                    |> LeaseStream.getLeaseEvents asOfDate.AsAt
                    |> List.choose (function LeaseEvent.PaymentReceived (_, p) -> Some p.PaymentId | _ -> None)
                    |> List.exists (fun pid -> pid = payment.PaymentId)
                if alreadyReceived then
                    sprintf "lease-%s payment-%s already received" leaseIdStr (payment.PaymentId |> PaymentId.toStringN)
                    |> DuplicateCommandException
                    |> raise
                let ctx = createEventContext effDate leaseStream.NextEventId
                let paymentReceived = LeaseEvent.PaymentReceived (ctx, payment)
                let effOrderOpt = paymentReceived |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> failwithf "cannot receive payment; lease-%s does not exist" leaseIdStr
                | Some _ -> paymentReceived |> LeaseEvent.toStoredEvent |> List.singleton
            | TerminateLease effDate ->
                let asOfDate = getAsOfDate effDate
                let alreadyTerminated =
                    leaseStream
                    |> LeaseStream.getLeaseEvents asOfDate.AsAt
                    |> List.exists (function LeaseEvent.LeaseTerminated _ -> true | _ -> false)
                if alreadyTerminated then
                    sprintf "lease-%s already terminated" leaseIdStr
                    |> DuplicateCommandException
                    |> raise
                let ctx = createEventContext effDate leaseStream.NextEventId
                let leaseTerminated = LeaseEvent.LeaseTerminated ctx
                let effOrderOpt = leaseTerminated |> LeaseEvent.getEventEffectiveOrder |> Some
                match reconstitute' asOfDate effOrderOpt with
                | None -> failwithf "cannot terminate lease; lease-%s does not exist" leaseIdStr
                | Some { LeaseStatus = leaseStatus } ->
                    match leaseStatus with
                    | Terminated -> failwithf "cannot terminate lease; lease-%s is already terminated" leaseIdStr
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
            | LeaseEvent.LeaseCreated (ctx, newLease) ->
                (ctx, newLease) :: leaseCreatedList
            | _ -> leaseCreatedList
        | DeletedEvent (_, deletedEventId) ->
            leaseCreatedList
            |> List.filter (fun (ctx, _) -> ctx.EventId <> deletedEventId)
        | _ -> leaseCreatedList

let foldLeaseCreatedList
    : LeaseCreatedList -> seq<StoredEvent> -> LeaseCreatedList =
    fun leaseCreatedList storedEvents ->
        storedEvents |> Seq.fold evolveLeaseCreatedList leaseCreatedList
