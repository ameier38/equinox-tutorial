module Lease.Aggregate

open FSharp.UMX
open System

module EventContext =
    let create 
        (getUtcNow: unit -> DateTimeOffset) =
        fun eventEffectiveAt ->
            { EventCreatedAt = getUtcNow() |> UMX.tag<eventCreatedAt>
              EventEffectiveAt = eventEffectiveAt }

module StoredLeaseEvent =
    let getEventContext (storedEvent:StoredLeaseEvent): EventContext =
        match storedEvent with
        | LeaseAccepted p -> p.EventContext
        | PaymentRequested p -> p.EventContext
        | PaymentSettled p -> p.EventContext
        | PaymentReturned p -> p.EventContext
        | VehicleReturned p -> p.EventContext
    let getEventEffectiveAt (storedEvent:StoredLeaseEvent): EventEffectiveAt =
        let ctx = getEventContext storedEvent
        ctx.EventEffectiveAt
    let getEventEffectiveOrder (storedEvent:StoredLeaseEvent): EventEffectiveOrder =
        match storedEvent with
        | LeaseAccepted _ -> %1
        | PaymentRequested _ -> %2
        | PaymentSettled _ -> %3
        | PaymentReturned _ -> %4
        | VehicleReturned _ -> %5
    let asAtOrBefore 
        (asAt:EventCreatedAt) = 
        fun (storedEvent:StoredLeaseEvent) ->
            let ctx = getEventContext storedEvent
            ctx.EventCreatedAt <= asAt
    let asOfOrBefore 
        (asOf:EventEffectiveAt)
        (orderOpt:EventEffectiveOrder option) =
        fun (storedEvent:StoredLeaseEvent) ->
            let ctx = getEventContext storedEvent
            let eventEffOrder = getEventEffectiveOrder storedEvent
            match orderOpt with
            | Some order when ctx.EventEffectiveAt = asOf ->
                (eventEffOrder <= order)
            | _ ->
                (ctx.EventEffectiveAt <= asOf)

module LeaseEvent =
    let getEventType (event:LeaseEvent): EventType =
        match event with
        | DayStarted _ -> %"DayStarted"
        | StoredLeaseEvent storedEvent ->
            match storedEvent with
            | LeaseAccepted _ -> %"LeaseAccepted"
            | PaymentRequested _ -> %"PaymentRequested"
            | PaymentSettled _ -> %"PaymentSettled"
            | PaymentReturned _ -> %"PaymentReturned"
            | VehicleReturned _ -> %"VehicleReturned"
        | DayEnded _ -> %"DayEnded"
    let getEventContext (event:LeaseEvent): EventContext =
        match event with
        | DayStarted ctx -> ctx
        | StoredLeaseEvent storedEvent -> StoredLeaseEvent.getEventContext storedEvent
        | DayEnded ctx -> ctx
    let getEventEffectiveAt (event:LeaseEvent): EventEffectiveAt =
        let ctx = getEventContext event
        ctx.EventEffectiveAt
    let getEventEffectiveOrder (event:LeaseEvent): EventEffectiveOrder =
        match event with
        | DayStarted _ -> %0
        | StoredLeaseEvent storedEvent -> StoredLeaseEvent.getEventEffectiveOrder storedEvent
        | DayEnded _ -> %6
    let getSortOrder (event:LeaseEvent) = 
        let ctx = getEventContext event
        let order = getEventEffectiveOrder event
        ctx.EventEffectiveAt, order, ctx.EventCreatedAt
    let getObservation (event:LeaseEvent) =
        let ctx = getEventContext event
        { ObservedAt = ctx.EventEffectiveAt
          ObservationOrder = getEventEffectiveOrder event
          ObservationDescription = getEventType event }

module LeaseState =
    let initial =
        { Observation = None
          AcceptedLease = None
          ReturnedVehicle = None
          Payments = Map.empty
          DaysPastDue = 0<day>
          PaymentAmountScheduled = 0m<usd>
          PaymentAmountScheduledHistory = []
          PaymentAmountReceived = 0m<usd>
          PaymentAmountChargedOff = 0m<usd>
          PaymentAmountOutstanding = 0m<usd>
          PaymentAmountCredit = 0m<usd>
          PaymentAmountUnpaid = 0m<usd> }

let evolveLeaseState : LeaseState -> LeaseEvent -> LeaseState =
    fun leaseState leaseEvent ->
        let obs = leaseEvent |> LeaseEvent.getObservation
        let leaseState = { leaseState with Observation = Some obs }
        match leaseEvent with
        | DayStarted payload ->
            let dayStarted = payload.EventEffectiveAt |> UMX.untag
            let paymentAmountScheduled, paymentAmountScheduledHistory =
                match leaseState.AcceptedLease with
                | Some acceptedLease ->
                    if dayStarted.Day = 15 then
                        let paymentAmount = acceptedLease.MonthlyPaymentAmount * 1m<month>
                        let paymentAmountScheduled = (leaseState.PaymentAmountScheduled + paymentAmount) |> Decimal.round 2
                        let paymentDate = dayStarted |> DateTimeOffset.toEndOfDay
                        let history = (paymentDate, paymentAmountScheduled) :: leaseState.PaymentAmountScheduledHistory
                        paymentAmount, history
                    else 0m<usd>, leaseState.PaymentAmountScheduledHistory
                | None -> 0m<usd>, leaseState.PaymentAmountScheduledHistory
            { leaseState with
                PaymentAmountScheduled = leaseState.PaymentAmountScheduled + paymentAmountScheduled
                PaymentAmountScheduledHistory = paymentAmountScheduledHistory
                PaymentAmountOutstanding = leaseState.PaymentAmountOutstanding + paymentAmountScheduled }
        | StoredLeaseEvent storedEvent ->
            match storedEvent with
            | LeaseAccepted payload ->
                { leaseState with
                    AcceptedLease = Some payload.AcceptedLease }
            | PaymentRequested payload ->
                let transactionId = payload.RequestedPayment.TransactionId
                let pmtState =
                    {| PaymentAmount = payload.RequestedPayment.RequestedAmount
                       RequestedAt = payload.RequestedPayment.RequestedAt |}
                    |> PaymentState.Requested
                { leaseState with
                    Payments = leaseState.Payments |> Map.add transactionId pmtState }
            | PaymentSettled payload ->
                let transactionId = payload.SettledPayment.TransactionId
                let pmtState = leaseState.Payments |> Map.find transactionId
                let newPmtState =
                    match pmtState with
                    | PaymentState.Requested requestedPayment ->
                        {| requestedPayment with
                            SettledAt = payload.SettledPayment.SettledAt |}
                        |> PaymentState.Settled
                    | other -> failwithf "cannot settle payment in leaseState %A" other
                { leaseState with
                    Payments = leaseState.Payments |> Map.add transactionId newPmtState }
            | PaymentReturned payload ->
                let transactionId = payload.ReturnedPayment.TransactionId
                let pmtState = leaseState.Payments |> Map.find transactionId
                let newPmtState =
                    match pmtState with
                    | PaymentState.Settled settledPayment ->
                        {| settledPayment with
                            ReceivedAt = None
                            ReturnedAt = payload.ReturnedPayment.ReturnedAt
                            ReturnedReason = payload.ReturnedPayment.ReturnedReason |}
                        |> PaymentState.Returned 
                    | PaymentState.Received receivedPayment ->
                        {| receivedPayment with
                            ReceivedAt = Some receivedPayment.ReceivedAt
                            ReturnedAt = payload.ReturnedPayment.ReturnedAt
                            ReturnedReason = payload.ReturnedPayment.ReturnedReason |}
                        |> PaymentState.Returned 
                    | other -> failwithf "cannot return payment in leaseState %A" other
                { leaseState with
                    Payments = leaseState.Payments |> Map.add transactionId newPmtState }
            | VehicleReturned payload ->
                { leaseState with
                    ReturnedVehicle = Some payload.ReturnedVehicle }
        | DayEnded payload ->
            let dayEnded = payload.EventEffectiveAt |> UMX.untag
            let newPayments =
                leaseState.Payments
                |> Map.map (fun _ pmtState -> 
                    match pmtState with
                    | PaymentState.Settled settledPmt ->
                        if (dayEnded - settledPmt.RequestedAt).Days > 3 then
                            {| settledPmt with
                                ReceivedAt = dayEnded |}
                            |> PaymentState.Received
                        else pmtState
                    | _ -> pmtState)
            let paymentAmountReceived =
                newPayments
                |> Map.fold (fun acc _ pmtState ->
                    let receivedAmount =
                        match pmtState with
                        | PaymentState.Received receivedPmt -> receivedPmt.PaymentAmount
                        | _ -> 0m<usd>
                    acc + receivedAmount
                ) 0m<usd>
            let paymentAmountOutstanding = (leaseState.PaymentAmountScheduled - paymentAmountReceived) |> max 0m<usd>
            let paymentAmountCredit = (paymentAmountReceived - leaseState.PaymentAmountScheduled) |> max 0m<usd>
            let defaultLastCurrentDate =
                leaseState.AcceptedLease
                |> Option.map (fun lease -> lease.AcceptedAt)
                |> Option.defaultValue dayEnded
            let lastCurrentDate =
                if paymentAmountReceived < leaseState.PaymentAmountScheduled then
                    leaseState.PaymentAmountScheduledHistory
                    |> List.pairwise
                    |> List.tryPick (fun ((_, pmtAmount), (nextPmtDate, _)) ->
                        if paymentAmountReceived >= pmtAmount then
                            nextPmtDate.AddDays(-1.0) |> Some
                        else None)
                    |> Option.defaultValue defaultLastCurrentDate
                else dayEnded
            let daysPastDue = (dayEnded - lastCurrentDate).Days |> UMX.tag<day>
            { leaseState with
                Payments = newPayments
                DaysPastDue = daysPastDue
                PaymentAmountReceived = paymentAmountReceived
                PaymentAmountOutstanding = paymentAmountOutstanding
                PaymentAmountCredit = paymentAmountCredit }

let resample
    (getUtcNow:unit -> DateTimeOffset)
    (asOn:AsOn)
    : StoredLeaseStream -> LeaseStream =
    fun storedLeaseStream ->
        let periodStart =
            storedLeaseStream
            |> List.map StoredLeaseEvent.getEventEffectiveAt
            |> List.min
            |> DateTimeOffset.addDays 1.0
        let periodEnd = asOn.AsOf
        let tickEvents =
            DateTimeOffset.range %periodStart %periodEnd
            |> Seq.collect (fun dt ->
                let dayStarted =
                    dt
                    |> UMX.tag<eventEffectiveAt>
                    |> EventContext.create getUtcNow
                    |> DayStarted
                let dayEnded =
                    dt
                    |> DateTimeOffset.toEndOfDay
                    |> UMX.tag<eventEffectiveAt>
                    |> EventContext.create getUtcNow
                    |> DayEnded
                seq { dayStarted; dayEnded })
            |> Seq.toList
        let leaseEvents =
            storedLeaseStream
            |> List.map StoredLeaseEvent
        tickEvents @ leaseEvents
            
let reconstitute
    (getUtcNow:unit -> DateTimeOffset)
    (asOn:AsOn)
    (effOrderOpt: EventEffectiveOrder option) 
    : StoredLeaseStream -> LeaseState =
    fun storedLeaseStream ->
        storedLeaseStream
        |> List.filter (StoredLeaseEvent.asAtOrBefore asOn.AsAt)
        |> List.filter (StoredLeaseEvent.asOfOrBefore asOn.AsOf effOrderOpt)
        |> resample getUtcNow asOn
        |> List.sortBy LeaseEvent.getSortOrder
        |> List.fold evolveLeaseState LeaseState.initial

let interpret
    (getUtcNow:unit -> DateTimeOffset)
    (leaseId:LeaseId)
    : LeaseCommand -> StoredLeaseStream -> StoredLeaseEvent list =
    fun leaseCommand storedLeaseStream ->
        let leaseIdStr = leaseId |> Guid.toStringN
        let getAsOn (effAt:EventEffectiveAt) =
            { AsAt = %getUtcNow(); AsOf = effAt }
        let createEventContext (effAt:EventEffectiveAt) = 
            EventContext.create getUtcNow effAt
        let reconstitute' asOn effOrderOpt =
            storedLeaseStream
            |> reconstitute getUtcNow asOn effOrderOpt
        match leaseCommand with
        | AcceptLease acceptedLease ->
            let effAt = acceptedLease.AcceptedAt |> UMX.tag<eventEffectiveAt>
            let eventContext = createEventContext effAt
            let leaseAccepted = LeaseAccepted {| EventContext = eventContext; AcceptedLease = acceptedLease |}
            let effOrderOpt = StoredLeaseEvent.getEventEffectiveOrder leaseAccepted |> Some
            let asOn = getAsOn effAt
            let leaseState = reconstitute' asOn effOrderOpt
            match leaseState.AcceptedLease with
            | None ->
                leaseAccepted
                |> List.singleton
            | Some _ ->
                sprintf "cannot accept lease; Lease-%s already accepted" leaseIdStr
                |> RpcException.raiseAlreadyExists
        | RequestPayment requestedPayment ->
            let transactionId = requestedPayment.TransactionId
            let transactionIdStr = transactionId |> Guid.toStringN
            let effAt = requestedPayment.RequestedAt |> UMX.tag<eventEffectiveAt>
            let eventContext = createEventContext effAt
            let paymentRequested = PaymentRequested {| EventContext = eventContext; RequestedPayment = requestedPayment |}
            let effOrderOpt = StoredLeaseEvent.getEventEffectiveOrder paymentRequested |> Some
            let asOn = getAsOn effAt
            let leaseState = reconstitute' asOn effOrderOpt
            match leaseState.Payments |> Map.tryFind transactionId with
            | None ->
                paymentRequested
                |> List.singleton
            | Some _ ->
                sprintf "cannot request payment; Lease-%s Transaction-%s already requested"
                    leaseIdStr transactionIdStr
                |> RpcException.raiseAlreadyExists
        | SettlePayment settledPayment ->
            let transactionId = settledPayment.TransactionId
            let transactionIdStr = transactionId |> Guid.toStringN
            let effAt = settledPayment.SettledAt |> UMX.tag<eventEffectiveAt>
            let eventContext = createEventContext effAt
            let paymentSettled = PaymentSettled {| EventContext = eventContext; SettledPayment = settledPayment |}
            let effOrderOpt = StoredLeaseEvent.getEventEffectiveOrder paymentSettled |> Some
            let asOn = getAsOn effAt
            let leaseState = reconstitute' asOn effOrderOpt
            match leaseState.Payments |> Map.tryFind transactionId with
            | Some pmtState ->
                match pmtState with
                | PaymentState.Requested _ ->
                    paymentSettled
                    |> List.singleton
                | other ->
                    sprintf "cannot settle payment; Lease-%s Transaction-%s not in requested state: %A"
                        leaseIdStr transactionIdStr other
                    |> RpcException.raiseInternal
            | None ->
                sprintf "cannot settle payment; Lease-%s Transaction-%s was not requested"
                    leaseIdStr transactionIdStr
                |> RpcException.raiseAlreadyExists
        | ReturnPayment returnedPayment ->
            let transactionId = returnedPayment.TransactionId
            let transactionIdStr = transactionId |> Guid.toStringN
            let effAt = returnedPayment.ReturnedAt |> UMX.tag<eventEffectiveAt>
            let eventContext = createEventContext effAt
            let paymentReturned = PaymentReturned {| EventContext = eventContext; ReturnedPayment = returnedPayment |}
            let effOrderOpt = StoredLeaseEvent.getEventEffectiveOrder paymentReturned |> Some
            let asOn = getAsOn effAt
            let leaseState = reconstitute' asOn effOrderOpt
            match leaseState.Payments |> Map.tryFind transactionId with
            | Some pmtState ->
                match pmtState with
                | PaymentState.Settled _
                | PaymentState.Received _ ->
                    paymentReturned
                    |> List.singleton
                | other ->
                    sprintf "cannot return payment; Lease-%s Transaction-%s not in settled or received state: %A"
                        leaseIdStr transactionIdStr other
                    |> RpcException.raiseInternal
            | None ->
                sprintf "cannot return payment; Lease-%s Transaction-%s was not requested"
                    leaseIdStr transactionIdStr
                |> RpcException.raiseAlreadyExists
        | ReturnVehicle returnedVehicle ->
            let vehicleIdStr = returnedVehicle.VehicleId |> Guid.toStringN
            let effAt = returnedVehicle.ReturnedAt |> UMX.tag<eventEffectiveAt>
            let eventContext = createEventContext effAt
            let vehicleReturned = VehicleReturned {| EventContext = eventContext; ReturnedVehicle = returnedVehicle |}
            let effOrderOpt = StoredLeaseEvent.getEventEffectiveOrder vehicleReturned |> Some
            let asOn = getAsOn effAt
            let leaseState = reconstitute' asOn effOrderOpt
            match leaseState.ReturnedVehicle with
            | None ->
                vehicleReturned
                |> List.singleton
            | Some _ ->
                sprintf "cannot return vehicle; Lease-%s Vehicle-%s already returned"
                    leaseIdStr vehicleIdStr
                |> RpcException.raiseAlreadyExists


let evolveStoredLeaseStream
    : StoredLeaseStream -> StoredLeaseEvent -> StoredLeaseStream =
    fun storedLeaseStream storedLeaseEvent ->
        storedLeaseEvent :: storedLeaseStream

let foldStoredLeaseStream
    : StoredLeaseStream -> seq<StoredLeaseEvent> -> StoredLeaseStream =
    Seq.fold evolveStoredLeaseStream
