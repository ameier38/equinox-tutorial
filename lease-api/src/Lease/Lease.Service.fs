module Lease.Service

open Lease
open Lease.Store
open FSharp.UMX
open Grpc.Core
open Serilog.Core
open System.Threading.Tasks

let (!!) (value:decimal<'u>) = %value |> Money.fromUSD
let (!@) (value:DateTime<'u>) = %value |> Google.Type.Date.FromDateTime
let (!@@) (value:DateTime<'u>) = %value |> DateTime.toUtc |> Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime

module PageToken =
    let decode (t:string) : int =
        match t with
        | "" -> 1
        | token -> token |> String.fromBase64 |> int 
    let encode (cursor:int) : string =
        cursor |> string |> String.toBase64

module Pagination =
    let getPage (pageToken:string) (pageSize:int) (s:seq<'T>) =
        let cursor = pageToken |> PageToken.decode
        let total = s |> Seq.length
        let toSkip = cursor - 1
        let remaining = total - toSkip
        let toTake = min remaining pageSize
        let page = s |> Seq.skip toSkip |> Seq.take toTake
        let nextPageToken = 
            match cursor + pageSize with
            | newCursor when newCursor > total -> ""
            | newCursor -> newCursor |> PageToken.encode
        nextPageToken, page

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
          StartDate = proto.StartDate.ToDateTime() |> UMX.tag<leaseStartDate>
          MaturityDate = proto.MaturityDate.ToDateTime() |> UMX.tag<leaseMaturityDate>
          MonthlyPaymentAmount = proto.MonthlyPaymentAmount |> Money.toUSD |> fun usd -> usd * 1M<1/month> }

module LeaseEvent =
    let toProto (leaseEvent:LeaseEvent) =
        let { EventId = eventId 
              EventCreatedTime = createdTime 
              EventEffectiveDate = effDate } = leaseEvent |> Aggregate.LeaseEvent.getEventContext
        let eventType = leaseEvent |> Aggregate.LeaseEvent.getEventType
        Tutorial.Lease.V1.LeaseEvent(
            EventId = %eventId,
            EventCreatedTime = !@@createdTime,
            EventEffectiveDate = !@effDate,
            EventType = %eventType)

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
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let pageSize = req.PageSize
            let pageToken = req.PageToken
            let! leases = listLeases asOfDate
            let nextPageToken, pageLeases = leases |> Pagination.getPage pageToken pageSize
            let res = Tutorial.Lease.V1.ListLeasesResponse(NextPageToken = nextPageToken)
            res.Leases.AddRange(pageLeases |> Seq.map Lease.toProto)
            return res
        } |> Async.StartAsTask

    override __.ListLeaseEvents(req:Tutorial.Lease.V1.ListLeaseEventsRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.ListLeaseEventsResponse> =
        async {
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let leaseId = req.LeaseId |> LeaseId.parse
            let pageSize = req.PageSize
            let pageToken = req.PageToken
            let! leaseEvents = listLeaseEvents leaseId asOfDate
            if leaseEvents |> List.isEmpty then
                let msg = sprintf "could not find lease-%s" req.LeaseId
                RpcException(Status(StatusCode.NotFound, msg))
                |> raise
            let nextPageToken, pageLeaseEvents = leaseEvents |> Pagination.getPage pageToken pageSize
            let res = Tutorial.Lease.V1.ListLeaseEventsResponse(NextPageToken = nextPageToken)
            res.Events.AddRange(pageLeaseEvents |> Seq.map LeaseEvent.toProto)
            return res
        } |> Async.StartAsTask
    
    override __.GetLease(req:Tutorial.Lease.V1.GetLeaseRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.GetLeaseResponse> =
        async {
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
