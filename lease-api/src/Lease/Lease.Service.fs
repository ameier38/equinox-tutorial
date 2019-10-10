module Lease.Service

open Lease
open Lease.Operators
open Lease.Store
open FSharp.UMX
open Grpc.Core
open Serilog.Core
open System.Threading.Tasks

module AsOf =
    let fromProto (proto:Tutorial.Lease.V1.AsOf) =
        { AsAt = %proto.AsAtTime.ToDateTime()
          AsOn = %proto.AsOnDate.ToDateTime() }

module Lease =
    let toProto (lease:Lease) =
        Tutorial.Lease.V1.Lease(
            LeaseId = (lease.LeaseId |> LeaseId.toStringN),
            UserId = (lease.UserId |> UserId.toStringN),
            CommencementDate = !@lease.CommencementDate,
            ExpirationDate = !@lease.ExpirationDate,
            MonthlyPaymentAmount = !!lease.MonthlyPaymentAmount)
    let fromProto (proto:Tutorial.Lease.V1.Lease) =
        let userId = proto.UserId |> UserId.parse
        { LeaseId = proto.LeaseId |> LeaseId.parse
          UserId = userId
          CommencementDate = proto.CommencementDate.ToDateTime()
          ExpirationDate = proto.ExpirationDate.ToDateTime()
          MonthlyPaymentAmount = proto.MonthlyPaymentAmount |> Money.toUSD |> fun usd -> usd * 1M<1/month> }

module LeaseEvent =
    let toProto (codec:FsCodec.IUnionEncoder<StoredEvent,byte[]>) (leaseEvent:LeaseEvent) =
        let ctx = leaseEvent |> Aggregate.LeaseEvent.getEventContext
        let eventType = leaseEvent |> Aggregate.LeaseEvent.getEventType
        let eventPayload = 
            leaseEvent 
            |> Aggregate.LeaseEvent.toStoredEvent 
            |> codec.Encode
        Tutorial.Lease.V1.LeaseEvent(
            EventId = %ctx.EventId,
            EventCreatedTime = !@@ctx.EventCreatedTime,
            EventEffectiveDate = !@ctx.EventEffectiveDate,
            EventType = %eventType,
            EventPayload = (eventPayload.Data |> String.fromBytes))

module LeaseStatus =
    let fromProto (proto:Tutorial.Lease.V1.LeaseStatus) =
        match proto with
        | Tutorial.Lease.V1.LeaseStatus.Outstanding -> Outstanding |> Ok
        | Tutorial.Lease.V1.LeaseStatus.Terminated -> Terminated |> Ok
        | other -> sprintf "invalid LeaseStatus %A" other |> Error
    let toProto (leaseStatus:LeaseStatus) =
        match leaseStatus with
        | Outstanding -> Tutorial.Lease.V1.LeaseStatus.Outstanding
        | Terminated -> Tutorial.Lease.V1.LeaseStatus.Terminated

module ScheduledPayment =
    let fromProto (proto:Tutorial.Lease.V1.ScheduledPayment) =
        { PaymentId = proto.PaymentId |> PaymentId.parse
          ScheduledDate = proto.ScheduledDate.ToDateTime()
          ScheduledAmount = proto.ScheduledAmount |> Money.toUSD }

module ReceivedPayment =
    let fromProto(proto:Tutorial.Lease.V1.ReceivedPayment) =
        { PaymentId = proto.PaymentId |> PaymentId.parse
          ReceivedDate = proto.ReceivedDate.ToDateTime()
          ReceivedAmount = proto.ReceivedAmount |> Money.toUSD }

module Termination =
    let fromProto (proto:Tutorial.Lease.V1.Termination) =
        { TerminationDate = proto.TerminationDate.ToDateTime()
          TerminationReason = proto.TerminationReason }

module LeaseObservation =
    let toProto (leaseObs:LeaseObservation) =
        Tutorial.Lease.V1.LeaseObservation(
            CreatedAtTime = !@@leaseObs.CreatedAt,
            UpdatedAtTime = !@@leaseObs.UpdatedAt,
            UpdatedOnDate = !@leaseObs.UpdatedOn,
            LeaseId = (leaseObs.LeaseId |> LeaseId.toStringN),
            UserId = (leaseObs.UserId |> UserId.toStringN),
            CommencementDate = !@leaseObs.CommencementDate,
            ExpirationDate = !@leaseObs.ExpirationDate,
            MonthlyPaymentAmount = !!leaseObs.MonthlyPaymentAmount,
            TotalScheduled = !!leaseObs.TotalScheduled,
            TotalPaid = !!leaseObs.TotalPaid,
            AmountDue = !!leaseObs.AmountDue,
            LeaseStatus = (leaseObs.LeaseStatus |> LeaseStatus.toProto),
            TerminatedDate = match leaseObs.TerminatedDate with Some d -> !@d | None -> null)

type LeaseAPIImpl
    (   getUtcNow:unit -> System.DateTime,
        store:Store,
        codec:FsCodec.IUnionEncoder<StoredEvent,byte[]>,
        logger:Logger) =
    inherit Tutorial.Lease.V1.LeaseAPI.LeaseAPIBase()

    let leaseResolver = StreamResolver(store, codec, "Lease", Aggregate.foldLeaseStream, Aggregate.initialLeaseStream)
    let leaseEventListResolver = StreamResolver(store, codec, "LeaseEventList", Aggregate.foldLeaseEventList, [])
    let (|LeaseStreamId|) (leaseId: LeaseId) = Equinox.AggregateId("Lease", LeaseId.toStringN leaseId)
    let (|LeaseStream|) (LeaseStreamId leaseId) = Equinox.Stream(logger, leaseResolver.Resolve leaseId, 3)
    let (|LeaseEventListStream|) (LeaseStreamId leaseId) = Equinox.Stream(logger, leaseEventListResolver.Resolve leaseId, 3)
    let listLeaseEvents (LeaseEventListStream stream) (asOf:AsOf) =
        stream.Query(fun leaseEventList -> 
            leaseEventList 
            |> List.filter (Aggregate.LeaseEvent.asAtOrBefore asOf.AsAt)
            |> List.filter (Aggregate.LeaseEvent.asOnOrBefore asOf.AsOn None))
    let getLease ((LeaseStream stream) as leaseId) (asOf:AsOf) =
        stream.Query(Aggregate.reconstitute leaseId asOf None)
    let execute ((LeaseStream stream) as leaseId) (command:Command) =
        stream.Transact(Aggregate.interpret getUtcNow leaseId command)

    override __.DeleteLeaseEvent(req:Tutorial.Lease.V1.DeleteLeaseEventRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.DeleteLeaseEventResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let eventId = req.EventId |> UMX.tag<eventId>
            let command = eventId |> DeleteEvent
            do! execute leaseId command
            let msg = sprintf "successfully deleted Event-%d for lease-%s" req.EventId req.LeaseId
            let res = Tutorial.Lease.V1.DeleteLeaseEventResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.ListLeases(req:Tutorial.Lease.V1.ListLeasesRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.ListLeasesResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let pageToken = req.PageToken |> UMX.tag<pageToken>
            let streamStart = pageToken |> PageToken.decode |> int64
            let! streamSlice = store.ReadStream("LeaseCreated", streamStart, req.PageSize)
            let totalCount = streamSlice.LastEventNumber + 1L |> int
            let prevPageToken =
                match (streamStart |> int) - req.PageSize with
                | c when c <= 0 -> "" |> UMX.tag<pageToken>
                | c -> c |> PageToken.encode
            let nextPageToken = 
                if streamSlice.IsEndOfStream then "" |> UMX.tag<pageToken> 
                else streamSlice.NextEventNumber |> int |> PageToken.encode
            let tryDecode (resolvedEvent:EventStore.ClientAPI.ResolvedEvent) =
                resolvedEvent
                |> Equinox.EventStore.UnionEncoderAdapters.encodedEventOfResolvedEvent
                |> codec.TryDecode
            let tryGetLeaseCreated = function
                | LeaseEvent.LeaseCreated (_, lease) -> Some lease
                | _ -> None
            let leases =
                streamSlice.Events
                |> Seq.choose (
                    tryDecode 
                    >> Option.bind Aggregate.StoredEvent.tryToLeaseEvent
                    >> Option.bind tryGetLeaseCreated)
                |> Seq.map Lease.toProto
            let res = 
                Tutorial.Lease.V1.ListLeasesResponse(
                    PrevPageToken = %prevPageToken,
                    NextPageToken = %nextPageToken,
                    TotalCount = totalCount)
            res.Leases.AddRange(leases)
            return res
        } |> Async.StartAsTask

    override __.ListLeaseEvents(req:Tutorial.Lease.V1.ListLeaseEventsRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.ListLeaseEventsResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let asOf = req.AsOf |> AsOf.fromProto
            let leaseId = req.LeaseId |> LeaseId.parse
            let pageSize = req.PageSize |> UMX.tag<pageSize>
            let pageToken = req.PageToken |> UMX.tag<pageToken>
            let! leaseEvents = listLeaseEvents leaseId asOf
            let totalCount = leaseEvents |> List.length
            let pageInfo = leaseEvents |> Pagination.getPage pageToken pageSize
            let res = 
                Tutorial.Lease.V1.ListLeaseEventsResponse(
                    NextPageToken = %pageInfo.NextPageToken,
                    TotalCount = totalCount)
            res.Events.AddRange(pageInfo.Page |> Seq.map (LeaseEvent.toProto codec))
            return res
        } |> Async.StartAsTask
    
    override __.GetLease(req:Tutorial.Lease.V1.GetLeaseRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.GetLeaseResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let asOf = req.AsOf |> AsOf.fromProto
            let leaseId = req.LeaseId |> LeaseId.parse
            let! leaseState = getLease leaseId asOf
            let res =
                match leaseState with
                | Some leaseObs -> 
                    Tutorial.Lease.V1.GetLeaseResponse(
                        Lease = (leaseObs |> LeaseObservation.toProto))
                | None ->
                    let msg = sprintf "could not find Lease-%s" req.LeaseId
                    RpcException(Status(StatusCode.NotFound, msg))
                    |> raise
            return res
        } |> Async.StartAsTask
    
    override __.CreateLease(req:Tutorial.Lease.V1.CreateLeaseRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.CreateLeaseResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let lease = req.Lease |> Lease.fromProto
            let command =
                lease
                |> CreateLease
                |> LeaseCommand
            do! execute lease.LeaseId command |> Async.Ignore
            let msg = sprintf "successfully created Lease-%s" req.Lease.LeaseId
            let res = Tutorial.Lease.V1.CreateLeaseResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.TerminateLease(req:Tutorial.Lease.V1.TerminateLeaseRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.TerminateLeaseResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let termination = req.Termination |> Termination.fromProto
            let command =
                termination
                |> TerminateLease
                |> LeaseCommand
            do! execute leaseId command |> Async.Ignore
            let msg = sprintf "successfully terminated Lease-%s" req.LeaseId
            let res = Tutorial.Lease.V1.TerminateLeaseResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.SchedulePayment(req:Tutorial.Lease.V1.SchedulePaymentRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.SchedulePaymentResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let payment = req.ScheduledPayment |> ScheduledPayment.fromProto
            let command =
                payment
                |> SchedulePayment
                |> LeaseCommand
            do! execute leaseId command |> Async.Ignore
            let msg = sprintf "successfully scheduled Payment-%s for Lease-%s" req.ScheduledPayment.PaymentId req.LeaseId
            let res = Tutorial.Lease.V1.SchedulePaymentResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.ReceivePayment(req:Tutorial.Lease.V1.ReceivePaymentRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.ReceivePaymentResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let payment = req.ReceivedPayment |> ReceivedPayment.fromProto
            let command =
                payment
                |> ReceivePayment
                |> LeaseCommand
            do! execute leaseId command |> Async.Ignore
            let msg = sprintf "successfully received Payment-%s for Lease-%s" req.ReceivedPayment.PaymentId req.LeaseId
            let res = Tutorial.Lease.V1.ReceivePaymentResponse(Message = msg)
            return res
        } |> Async.StartAsTask
