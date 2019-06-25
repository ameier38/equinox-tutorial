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
    let decode (t:PageToken) : int =
        match %t with
        | "" -> 1
        | token -> token |> String.fromBase64 |> int 
    let encode (cursor:int) : PageToken =
        cursor |> string |> String.toBase64 |> UMX.tag<pageToken>

module Pagination =
    let getPage (pageToken:PageToken) (pageSize:PageSize) (s:seq<'T>) =
        let cursor = pageToken |> PageToken.decode
        let total = s |> Seq.length
        let toSkip = cursor - 1
        let remaining = total - toSkip
        let toTake = min remaining %pageSize
        let page = s |> Seq.skip toSkip |> Seq.take toTake
        let nextPageToken = 
            match cursor + %pageSize with
            | newCursor when newCursor > total ->
                "" |> UMX.tag<pageToken>
            | newCursor ->
                newCursor |> PageToken.encode
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
          MonthlyPaymentAmount = proto.MonthlyPaymentAmount.DecimalValue |> UMX.tag<usd/month> }

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

    override __.ListLeases(req:Tutorial.Lease.V1.ListLeasesRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.ListLeasesResponse> =
        async {
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let! leases = listLeases asOfDate
            let res = Tutorial.Lease.V1.ListLeasesResponse()
            res.Leases.AddRange(leases |> List.map Lease.toProto)
            return res
        } |> Async.StartAsTask

    override __.ListLeaseEvents(req:Tutorial.Lease.V1.ListLeaseEventsRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.ListLeaseEventsResponse> =
        async {
            let asOfDate = req.AsOfDate |> AsOfDate.fromProto
            let leaseId = req.LeaseId |> LeaseId.parse
            let! leaseEvents = listLeaseEvents leaseId asOfDate
            let res = Tutorial.Lease.V1.ListLeaseEventsResponse()
            res.Events.AddRange(leaseEvents |> List.map LeaseEvent.toProto)
            return res
        } |> Async.StartAsTask
    
    override __.GetLease(req:Tutorial.Lease.V1.GetLeaseRequest, ctx:ServerCallContext) 
        : Task<Tutorial.Lease.V1.GetLeaseResponse> =
        ""
    
    override __.CreateLease(req:Tutorial.Lease.V1.CreateLeaseRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.CreateLeaseResponse> =
        ""

    override __.TerminateLease(req:Tutorial.Lease.V1.TerminateLeaseRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.TerminateLeaseResponse> =
        ""

    override __.SchedulePayment(req:Tutorial.Lease.V1.SchedulePaymentRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.SchedulePaymentResponse> =
        ""

    override __.ReceivePayment(req:Tutorial.Lease.V1.ReceivePaymentRequest, ctx:ServerCallContext)
        : Task<Tutorial.Lease.V1.ReceivePaymentResponse> =
        ""
