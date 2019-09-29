module Lease.Service

open Lease
open Lease.Operators
open Lease.Store
open FSharp.UMX
open Grpc.Core
open Serilog.Core
open System.Threading.Tasks

// ref: https://cloud.google.com/apis/design/design_patterns#list_pagination
// pageToken contains the zero-based index of the starting element
module Pagination =
    let getPage (pageToken:PageToken) (pageSize:PageSize) (s:seq<'T>) =
        let start = pageToken |> PageToken.decode
        let cnt = s |> Seq.length
        let remaining = cnt - start
        let toTake = min remaining %pageSize
        let page = s |> Seq.skip start |> Seq.take toTake
        let prevPageToken =
            match start - %pageSize with
            | c when c <= 0 -> "" |> UMX.tag<pageToken>
            | c -> c |> PageToken.encode
        let nextPageToken = 
            match start + %pageSize with
            | c when c >= cnt -> "" |> UMX.tag<pageToken>
            | c -> c |> PageToken.encode
        {| PrevPageToken = prevPageToken
           NextPageToken = nextPageToken
           Page = page |}

module AsOfDate =
    let fromProto (proto:Tutorial.Lease.V1.AsOfDate) =
        { AsAt = %proto.AsAtTime.ToDateTime()
          AsOn = %proto.AsOnDate.ToDateTime() }

module Lease =
    let toProto (lease:Lease) =
        Tutorial.Lease.V1.Lease(
            LeaseId = (lease.LeaseId |> LeaseId.toStringN),
            UserId = (lease.UserId |> UserId.toStringN),
            StartDate = !@lease.StartDate,
            MaturityDate = !@lease.MaturityDate,
            MonthlyPaymentAmount = !!lease.MonthlyPaymentAmount)
    let fromProto (proto:Tutorial.Lease.V1.Lease) =
        let userId = proto.UserId |> UserId.parse
        { LeaseId = proto.LeaseId |> LeaseId.parse
          UserId = userId
          StartDate = proto.StartDate.ToDateTime()
          MaturityDate = proto.MaturityDate.ToDateTime()
          MonthlyPaymentAmount = proto.MonthlyPaymentAmount |> Money.toUSD |> fun usd -> usd * 1M<1/month> }

module LeaseEvent =
    let toProto (codec:FsCodec.IUnionEncoder<StoredEvent,byte[]>) (leaseEvent:LeaseEvent) =
        let { EventId = eventId 
              EventCreatedTime = createdTime 
              EventEffectiveDate = effDate } = leaseEvent |> Aggregate.LeaseEvent.getEventContext
        let eventType = leaseEvent |> Aggregate.LeaseEvent.getEventType
        let eventPayload = 
            leaseEvent 
            |> Aggregate.LeaseEvent.toStoredEvent 
            |> codec.Encode
        Tutorial.Lease.V1.LeaseEvent(
            EventId = %eventId,
            EventCreatedTime = !@@createdTime,
            EventEffectiveDate = !@effDate,
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

module Payment =
    let fromProto (proto:Tutorial.Lease.V1.Payment) =
        { PaymentId = proto.PaymentId |> PaymentId.parse
          PaymentDate = proto.PaymentDate.ToDateTime()
          PaymentAmount = proto.PaymentAmount |> Money.toUSD }

module Termination =
    let fromProto (proto:Tutorial.Lease.V1.Termination) =
        { TerminationId = proto.TerminationId |> TerminationId.parse
          TerminationDate = proto.TerminationDate.ToDateTime()
          TerminationReason = proto.TerminationReason }

module LeaseObservation =
    let toProto (leaseObs:LeaseObservation) =
        let lease = leaseObs.Lease |> Lease.toProto
        Tutorial.Lease.V1.LeaseObservation(
            Lease = lease,
            CreatedTime = !@@leaseObs.CreatedTime,
            UpdatedTime = !@@leaseObs.UpdatedTime,
            TotalScheduled = !!leaseObs.TotalScheduled,
            TotalPaid = !!leaseObs.TotalPaid,
            AmountDue = !!leaseObs.AmountDue,
            LeaseStatus = (leaseObs.LeaseStatus |> LeaseStatus.toProto))

type LeaseAPIImpl
    (   getUtcNow:unit -> System.DateTime,
        store:Store,
        codec:FsCodec.IUnionEncoder<StoredEvent,byte[]>,
        logger:Logger) =
    inherit Tutorial.Lease.V1.LeaseAPI.LeaseAPIBase()

    let leaseResolver = StreamResolver(store, codec, "Lease", Aggregate.foldLeaseStream, Aggregate.initialLeaseStream)
    let leaseEventListResolver = StreamResolver(store, codec, "LeaseEventList", Aggregate.foldLeaseEventList, [])
    let leaseCreatedListResolver = StreamResolver(store, codec, "LeaseCreatedList", Aggregate.foldLeaseList, [])
    let leaseCreatedListStreamId = Equinox.StreamName("LeaseCreated")
    let leaseCreatedListStream = Equinox.Stream(logger, leaseCreatedListResolver.Resolve leaseCreatedListStreamId, 3)
    let (|LeaseStreamId|) (leaseId: LeaseId) = Equinox.AggregateId("Lease", LeaseId.toStringN leaseId)
    let (|LeaseStream|) (LeaseStreamId leaseId) = Equinox.Stream(logger, leaseResolver.Resolve leaseId, 3)
    let (|LeaseEventListStream|) (LeaseStreamId leaseId) = Equinox.Stream(logger, leaseEventListResolver.Resolve leaseId, 3)
    let listLeases (asOfDate:AsOfDate) =
        leaseCreatedListStream.Query(fun leaseCreatedList ->
            leaseCreatedList
            |> List.filter (fun (ctx, _) ->
                (ctx.EventCreatedTime <= asOfDate.AsAt) && (ctx.EventEffectiveDate <= asOfDate.AsOn))
            |> List.map snd)
    let listLeaseEvents (LeaseEventListStream stream) (asOf:AsOfDate) =
        stream.Query(fun leaseEventList -> 
            leaseEventList 
            |> List.filter (Aggregate.LeaseEvent.asAtOrBefore asOf.AsAt)
            |> List.filter (Aggregate.LeaseEvent.asOnOrBefore asOf.AsOn None))
    let getLease ((LeaseStream stream) as leaseId) (asOfDate:AsOfDate) =
        stream.Query(Aggregate.reconstitute leaseId asOfDate None)
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
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let pageSize = req.PageSize |> UMX.tag<pageSize>
            let pageToken = req.PageToken |> UMX.tag<pageToken>
            let! leases = listLeases asOfDate
            let totalCount = leases |> List.length
            let pageInfo = leases |> Pagination.getPage pageToken pageSize
            let res = 
                Tutorial.Lease.V1.ListLeasesResponse(
                    PrevPageToken = %pageInfo.PrevPageToken,
                    NextPageToken = %pageInfo.NextPageToken,
                    TotalCount = totalCount)
            res.Leases.AddRange(pageInfo.Page |> Seq.map Lease.toProto)
            return res
        } |> Async.StartAsTask

    override __.ListLeaseEvents(req:Tutorial.Lease.V1.ListLeaseEventsRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.ListLeaseEventsResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let leaseId = req.LeaseId |> LeaseId.parse
            let pageSize = req.PageSize |> UMX.tag<pageSize>
            let pageToken = req.PageToken |> UMX.tag<pageToken>
            let! leaseEvents = listLeaseEvents leaseId asOfDate
            let totalCount = leaseEvents |> List.length
            if totalCount = 0 then
                let msg = sprintf "could not find Lease-%s" req.LeaseId
                RpcException(Status(StatusCode.NotFound, msg))
                |> raise
            let pageInfo = leaseEvents |> Pagination.getPage pageToken pageSize
            let res = 
                Tutorial.Lease.V1.ListLeaseEventsResponse(
                    PrevPageToken = %pageInfo.PrevPageToken,
                    NextPageToken = %pageInfo.NextPageToken,
                    TotalCount = totalCount)
            res.Events.AddRange(pageInfo.Page |> Seq.map (LeaseEvent.toProto codec))
            return res
        } |> Async.StartAsTask
    
    override __.GetLease(req:Tutorial.Lease.V1.GetLeaseRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.GetLeaseResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let leaseId = req.LeaseId |> LeaseId.parse
            let! leaseState = getLease leaseId asOfDate
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
            let payment = req.Payment |> Payment.fromProto
            let command =
                payment
                |> SchedulePayment
                |> LeaseCommand
            do! execute leaseId command |> Async.Ignore
            let msg = sprintf "successfully scheduled Payment-%s for Lease-%s" req.Payment.PaymentId req.LeaseId
            let res = Tutorial.Lease.V1.SchedulePaymentResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.ReceivePayment(req:Tutorial.Lease.V1.ReceivePaymentRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.ReceivePaymentResponse> =
        async {
            logger.Debug(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let payment = req.Payment |> Payment.fromProto
            let command =
                payment
                |> ReceivePayment
                |> LeaseCommand
            do! execute leaseId command |> Async.Ignore
            let msg = sprintf "successfully received Payment-%s for Lease-%s" req.Payment.PaymentId req.LeaseId
            let res = Tutorial.Lease.V1.ReceivePaymentResponse(Message = msg)
            return res
        } |> Async.StartAsTask
