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
        let last = (s |> Seq.length) - 1
        let remaining = last - start
        let toTake = min remaining %pageSize
        let page = s |> Seq.skip start |> Seq.take toTake
        let prevPageToken =
            match start - %pageSize with
            | c when c < 0 -> "" |> UMX.tag<pageToken>
            | c -> c |> PageToken.encode
        let nextPageToken = 
            match start + %pageSize with
            | c when c > last -> "" |> UMX.tag<pageToken>
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
          PaymentDate = proto.PaymentDate.ToDateTime() |> UMX.tag<paymentDate>
          PaymentAmount = proto.PaymentAmount |> Money.toUSD }

module LeaseObservation =
    let toProto (leaseObs:LeaseObservation) =
        let lease = leaseObs.Lease |> Lease.toProto
        Tutorial.Lease.V1.LeaseObservation(
            Lease = lease,
            TotalScheduled = !!leaseObs.TotalScheduled,
            TotalPaid = !!leaseObs.TotalPaid,
            AmountDue = !!leaseObs.AmountDue,
            LeaseStatus = (leaseObs.LeaseStatus |> LeaseStatus.toProto))

type LeaseAPIImpl
    (   getUtcNow:unit -> System.DateTime,
        leaseResolver:StreamResolver<StoredEvent,LeaseStream>,
        leaseEventListResolver:StreamResolver<StoredEvent,LeaseEventList>,
        leaseListResolver:StreamResolver<StoredEvent,LeaseList>,
        logger:Logger) =
    inherit Tutorial.Lease.V1.LeaseAPI.LeaseAPIBase()

    let leaseListStreamId = Equinox.DeprecatedRawName("leases")
    let leaseListStream = Equinox.Stream(logger, leaseListResolver.Resolve leaseListStreamId, 3)
    let (|LeaseStreamId|) (leaseId: LeaseId) = Equinox.AggregateId("lease", LeaseId.toStringN leaseId)
    let (|LeaseStream|) (LeaseStreamId leaseId) = Equinox.Stream(logger, leaseResolver.Resolve leaseId, 3)
    let (|LeaseEventListStream|) (LeaseStreamId leaseId) = Equinox.Stream(logger, leaseEventListResolver.Resolve leaseId, 3)
    let listLeases (asOfDate:AsOfDate) =
        leaseListStream.Query(fun leaseList ->
            leaseList
            |> List.filter (fun (ctx, _) ->
                (ctx.EventCreatedTime <= asOfDate.AsAt) && (ctx.EventEffectiveDate <= asOfDate.AsOn))
            |> List.map snd)
    let listLeaseEvents (LeaseEventListStream stream) (asOfDate:AsOfDate) =
        stream.Query(fun leaseEventList -> 
            leaseEventList 
            |> List.filter (Aggregate.LeaseEvent.asOfOrBefore asOfDate None))
    let getLease ((LeaseStream stream) as leaseId) (asOfDate:AsOfDate) =
        stream.Query(Aggregate.reconstitute leaseId asOfDate None)
    let execute ((LeaseStream stream) as leaseId) (command:Command) =
        stream.Transact(Aggregate.interpret getUtcNow leaseId command)

    override __.DeleteLeaseEvent(req:Tutorial.Lease.V1.DeleteLeaseEventRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.DeleteLeaseEventResponse> =
        async {
            logger.Information(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let eventId = req.EventId |> UMX.tag<eventId>
            let command = eventId |> DeleteEvent
            do! execute leaseId command
            let msg = sprintf "successfully deleted event-%d for lease-%s" req.EventId req.LeaseId
            let res = Tutorial.Lease.V1.DeleteLeaseEventResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.ListLeases(req:Tutorial.Lease.V1.ListLeasesRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.ListLeasesResponse> =
        async {
            logger.Information(sprintf "received req: %A" req)
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let pageSize = req.PageSize
            let pageToken = req.PageToken
            let! leases = listLeases asOfDate
            let totalCount = leases |> List.length
            let pageInfo = leases |> Pagination.getPage pageToken pageSize
            let res = 
                Tutorial.Lease.V1.ListLeasesResponse(
                    PrevPageToken = pageInfo.PrevPageToken,
                    NextPageToken = pageInfo.NextPageToken,
                    TotalCount = totalCount)
            res.Leases.AddRange(pageInfo.Page |> Seq.map Lease.toProto)
            return res
        } |> Async.StartAsTask

    override __.ListLeaseEvents(req:Tutorial.Lease.V1.ListLeaseEventsRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.ListLeaseEventsResponse> =
        async {
            logger.Information(sprintf "received req: %A" req)
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let leaseId = req.LeaseId |> LeaseId.parse
            let pageSize = req.PageSize
            let pageToken = req.PageToken
            let! leaseEvents = listLeaseEvents leaseId asOfDate
            let totalCount = leaseEvents |> List.length
            if totalCount = 0 then
                let msg = sprintf "could not find lease-%s" req.LeaseId
                RpcException(Status(StatusCode.NotFound, msg))
                |> raise
            let pageInfo = leaseEvents |> Pagination.getPage pageToken pageSize
            let res = 
                Tutorial.Lease.V1.ListLeaseEventsResponse(
                    PrevPageToken = pageInfo.PrevPageToken,
                    NextPageToken = pageInfo.NextPageToken,
                    TotalCount = totalCount)
            res.Events.AddRange(pageInfo.Page |> Seq.map LeaseEvent.toProto)
            return res
        } |> Async.StartAsTask
    
    override __.GetLease(req:Tutorial.Lease.V1.GetLeaseRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.GetLeaseResponse> =
        async {
            logger.Information(sprintf "received req: %A" req)
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let leaseId = req.LeaseId |> LeaseId.parse
            let! leaseState = getLease leaseId asOfDate
            let res =
                match leaseState with
                | Some leaseObs -> 
                    Tutorial.Lease.V1.GetLeaseResponse(
                        Lease = (leaseObs |> LeaseObservation.toProto))
                | None ->
                    let msg = sprintf "could not find lease-%s" req.LeaseId
                    RpcException(Status(StatusCode.NotFound, msg))
                    |> raise
            return res
        } |> Async.StartAsTask
    
    override __.CreateLease(req:Tutorial.Lease.V1.CreateLeaseRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.CreateLeaseResponse> =
        async {
            logger.Information(sprintf "received req: %A" req)
            let effDate = req.Lease.StartDate.ToDateTime() |> UMX.tag<eventEffectiveDate>
            let lease = req.Lease |> Lease.fromProto
            let command =
                (effDate, lease)
                |> CreateLease
                |> LeaseCommand
            do! execute lease.LeaseId command |> Async.Ignore
            let msg = sprintf "successfully created lease-%s" req.Lease.LeaseId
            let res = Tutorial.Lease.V1.CreateLeaseResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.TerminateLease(req:Tutorial.Lease.V1.TerminateLeaseRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.TerminateLeaseResponse> =
        async {
            logger.Information(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let effDate = req.EffectiveDate.ToDateTime() |> UMX.tag<eventEffectiveDate>
            let command =
                effDate
                |> TerminateLease
                |> LeaseCommand
            do! execute leaseId command |> Async.Ignore
            let msg = sprintf "successfully terminated lease-%s" req.LeaseId
            let res = Tutorial.Lease.V1.TerminateLeaseResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.SchedulePayment(req:Tutorial.Lease.V1.SchedulePaymentRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.SchedulePaymentResponse> =
        async {
            logger.Information(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let effDate = req.Payment.PaymentDate.ToDateTime() |> UMX.tag<eventEffectiveDate>
            let payment = req.Payment |> Payment.fromProto
            let command =
                (effDate, payment)
                |> SchedulePayment
                |> LeaseCommand
            do! execute leaseId command |> Async.Ignore
            let msg = sprintf "successfully scheduled payment-%s for lease-%s" req.Payment.PaymentId req.LeaseId
            let res = Tutorial.Lease.V1.SchedulePaymentResponse(Message = msg)
            return res
        } |> Async.StartAsTask

    override __.ReceivePayment(req:Tutorial.Lease.V1.ReceivePaymentRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.ReceivePaymentResponse> =
        async {
            logger.Information(sprintf "received req: %A" req)
            let leaseId = req.LeaseId |> LeaseId.parse
            let effDate = req.Payment.PaymentDate.ToDateTime() |> UMX.tag<eventEffectiveDate>
            let payment = req.Payment |> Payment.fromProto
            let command =
                (effDate, payment)
                |> ReceivePayment
                |> LeaseCommand
            do! execute leaseId command |> Async.Ignore
            let msg = sprintf "successfully received payment-%s for lease-%s" req.Payment.PaymentId req.LeaseId
            let res = Tutorial.Lease.V1.ReceivePaymentResponse(Message = msg)
            return res
        } |> Async.StartAsTask
