module Lease.Service

open Lease.Store
open FSharp.UMX
open Grpc.Core
open Serilog.Core
open System.Threading.Tasks

module AsOfDate =
    let fromProto (proto:Proto.Lease.AsOfDate) =
        { AsAt = %proto.AsAt.ToDateTime()
          AsOn = %proto.AsOn.ToDateTime() }

module LeaseQuery =
    let fromProto (proto:Proto.Lease.Query) =
        result {
            let! leaseId = proto.LeaseId |> LeaseId.tryParse |> Result.ofOption "could not parse leaseId"
            let asOfDate = proto.AsOfDate |> AsOfDate.fromProto
            return leaseId, asOfDate
        }

module NewLease =
    let fromProto leaseId (proto:Proto.Lease.NewLease) =
        result {
            let! userId = proto.UserId |> UserId.tryParse |> Result.ofOption "could not parse userId"
            return
                { LeaseId = leaseId
                  UserId = userId
                  MaturityDate = proto.MaturityDate.ToDateTime() |> UMX.tag<leaseMaturityDate>
                  MonthlyPaymentAmount = proto.MonthlyPaymentAmount |> decimal |> UMX.tag<usd/month> }
        }

module LeaseCommand =
    type CommandCase = Proto.Lease.LeaseCommand.CommandOneofCase
    let fromProto leaseId (proto:Proto.Lease.LeaseCommand) =
        let effDate = proto.EffectiveDate.ToDateTime() |> UMX.tag<eventEffectiveDate>
        match proto.CommandCase with
        | CommandCase.CreateLease ->
            result {
                let! newLease =
                    proto.CreateLease
                    |> NewLease.fromProto leaseId
                return CreateLease (effDate, newLease)
            }
        | CommandCase.SchedulePayment ->
            let paymentAmount = proto.SchedulePayment |> decimal |> UMX.tag<usd>
            SchedulePayment (effDate, paymentAmount) |> Ok
        | CommandCase.ReceivePayment ->
            let paymentAmount = proto.ReceivePayment |> decimal |> UMX.tag<usd>
            ReceivePayment (effDate, paymentAmount) |> Ok
        | CommandCase.TerminateLease ->
            TerminateLease effDate |> Ok
        | other -> sprintf "invalid command %A" other |> Error

module Command =
    type CommandCase = Proto.Lease.Command.CommandOneofCase
    let fromProto leaseId (proto:Proto.Lease.Command) =
        match proto.CommandCase with
        | CommandCase.DeleteEvent ->
            proto.DeleteEvent |> UMX.tag<eventId> |> DeleteEvent |> Ok
        | CommandCase.LeaseCommand ->
            proto.LeaseCommand |> LeaseCommand.fromProto leaseId |> Result.map LeaseCommand
        | other -> sprintf "invalid command %A" other |> Error

module LeaseEvent =
    let toProto (leaseEvent:LeaseEvent) =
        let { EventId = eventId 
              EventCreatedDate = createdDate 
              EventEffectiveDate = effDate } = leaseEvent |> Aggregate.LeaseEvent.getEventContext
        let eventType = leaseEvent |> Aggregate.LeaseEvent.getEventType
        Proto.Lease.LeaseEvent(
            EventId = (eventId |> UMX.untag),
            EventCreatedDate = (createdDate |> UMX.untag |> DateTime.toTimestamp),
            EventEffectiveDate = (effDate |> UMX.untag |> DateTime.toTimestamp),
            EventType = (eventType |> UMX.untag))

module LeaseStatus =
    let fromProto (proto:Proto.Lease.LeaseStatus) =
        match proto with
        | Proto.Lease.LeaseStatus.Outstanding -> LeaseStatus.Outstanding |> Ok
        | Proto.Lease.LeaseStatus.Terminated -> LeaseStatus.Terminated |> Ok
        | other -> sprintf "invalid LeaseStatus %A" other |> Error
    let toProto (leaseStatus:LeaseStatus) =
        match leaseStatus with
        | LeaseStatus.Outstanding -> Proto.Lease.LeaseStatus.Outstanding
        | LeaseStatus.Terminated -> Proto.Lease.LeaseStatus.Terminated

module LeaseObservation =
    let toProto (obsDate:EventEffectiveDate) (leaseObs:LeaseObservation) =
        Proto.Lease.LeaseObservation(
            ObservationDate = (obsDate |> UMX.untag |> DateTime.toTimestamp),
            LeaseId = (leaseObs.LeaseId |> LeaseId.toStringN),
            UserId = (leaseObs.UserId |> UserId.toStringN),
            StartDate = (leaseObs.StartDate |> UMX.untag |> DateTime.toTimestamp),
            MaturityDate = (leaseObs.MaturityDate |> UMX.untag |> DateTime.toTimestamp),
            MonthlyPaymentAmount = (leaseObs.MonthlyPaymentAmount |> UMX.untag |> Decimal.round 2 |> float32),
            TotalScheduled = (leaseObs.TotalScheduled |> UMX.untag |> Decimal.round 2 |> float32),
            TotalPaid = (leaseObs.TotalPaid |> UMX.untag |> Decimal.round 2 |> float32),
            AmountDue = (leaseObs.AmountDue |> UMX.untag |> Decimal.round 2 |> float32),
            LeaseStatus = (leaseObs.LeaseStatus |> LeaseStatus.toProto))

type LeaseServiceImpl(store:Store, logger:Logger) =
    inherit Proto.Lease.LeaseService.LeaseServiceBase()

    let (|AggregateId|) (leaseId: LeaseId) = Equinox.AggregateId("lease", LeaseId.toStringN leaseId)
    let (|Stream|) (AggregateId leaseId) = Equinox.Stream(logger, store.Resolve leaseId, 3)
    let execute (Stream stream) command = stream.Transact(Aggregate.decide command)
    let queryState (Stream stream) (asOfDate:AsOfDate) = stream.Query(Aggregate.reconstitute asOfDate None)
    let queryEvents (Stream stream) (asOfDate:AsOfDate) = stream.Query(Aggregate.StreamState.getEffectiveLeaseEvents asOfDate None)

    override __.Execute(request:Proto.Lease.Command, context:ServerCallContext)
        : Task<Proto.Lease.ExecuteResponse> =
        let success () = Proto.Lease.ExecuteResponse(Ok="Success!")
        let failure err = Proto.Lease.ExecuteResponse(Error=err)
        asyncResult {
            let! leaseId = 
                request.LeaseId 
                |> LeaseId.tryParse 
                |> Result.ofOption "cannot parse leaseId"
                |> AsyncResult.ofResult
            let! command = 
                request 
                |> Command.fromProto leaseId 
                |> AsyncResult.ofResult
            do! execute leaseId command
        }
        |> AsyncResult.bimap success failure
        |> Async.StartAsTask

    override __.QueryState(request:Proto.Lease.Query, context:ServerCallContext)
        : Task<Proto.Lease.QueryStateResponse> =
        let success res = Proto.Lease.QueryStateResponse(Ok=res)
        let failure err = Proto.Lease.QueryStateResponse(Error=err)
        asyncResult {
            let! (userId, asOfDate) = request |> LeaseQuery.fromProto |> AsyncResult.ofResult
            let! leaseObsOpt = queryState userId asOfDate
            return
                match leaseObsOpt with
                | Some leaseObs ->
                    let leaseObsProto = leaseObs |> LeaseObservation.toProto asOfDate.AsOn
                    Proto.Lease.QueryStateResponse.Types.LeaseState(Observation=leaseObsProto)
                | None ->
                    Proto.Lease.QueryStateResponse.Types.LeaseState()
        } 
        |> AsyncResult.bimap success failure
        |> Async.StartAsTask

    override __.QueryEvents(request:Proto.Lease.Query, context:ServerCallContext)
        : Task<Proto.Lease.QueryEventsResponse> =
        let success events = Proto.Lease.QueryEventsResponse(Ok=events)
        let failure err = Proto.Lease.QueryEventsResponse(Error=err)
        asyncResult {
            let! (userId, asOfDate) = request |> LeaseQuery.fromProto |> AsyncResult.ofResult
            let! leaseEvents = queryEvents userId asOfDate |> AsyncResult.ofAsync
            let leaseEventsProto = Proto.Lease.QueryEventsResponse.Types.LeaseEvents()
            leaseEventsProto.Events.AddRange(leaseEvents |> List.map LeaseEvent.toProto)
            return leaseEventsProto
        } 
        |> AsyncResult.bimap success failure
        |> Async.StartAsTask
