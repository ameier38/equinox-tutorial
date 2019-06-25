open Expecto
open Expecto.Flip
open FSharp.UMX
open FSharp.Control
open Google.Protobuf
open Grpc.Core
open Grpc.Core.Testing
open Grpc.Core.Utils
open Lease
open Lease.Store
open Lease.Service
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Serilog
open System
open System.Threading

let getUtcNow () = DateTime(2019, 6, 25)
let logger = LoggerConfiguration().WriteTo.Console().CreateLogger()
let config = Config.load()
let store = Store(config)
let serializationSettings = JsonSerializerSettings()
let codec = Equinox.Codec.NewtonsoftJson.Json.Create<StoredEvent>(serializationSettings)
let leaseResolver = StreamResolver(store, codec, "lease", Aggregate.foldLeaseStream, Aggregate.initialLeaseStream)
let leaseEventListResolver = StreamResolver(store, codec, "events", Aggregate.foldLeaseEventList, [])
let leaseListResolver = StreamResolver(store, codec, "leases", Aggregate.foldLeaseList, [])
let leaseAPI = LeaseAPIImpl(getUtcNow, leaseResolver, leaseEventListResolver, leaseListResolver, logger)

module DateTime =
    let toProtoDate (dt:DateTime) =
        Google.Type.Date.FromDateTime dt
    let toProtoTimestamp (dt:DateTime) =
        dt |> DateTime.toUtc |> WellKnownTypes.Timestamp.FromDateTime

let (!!) (value:decimal<'T>) = %value |> Money.fromUSD
let (!@) (value:DateTime<'T>) = %value |> DateTime.toUtc |> DateTime.toProtoDate
let (!@@) (value:DateTime<'T>) = %value |> DateTime.toUtc |> DateTime.toProtoTimestamp

let formatJson json =
    JValue.Parse(json).ToString(Formatting.Indented)

module Message =
    let toJson (message:IMessage) =
        let jsonFormatter = JsonFormatter(JsonFormatter.Settings(true))
        jsonFormatter.Format(message) |> formatJson

module Guid =
    let create () = Guid.NewGuid() |> Guid.toStringN

let serverCtx = 
    TestServerCallContext.Create(
        method = "test", 
        host = "localhost", 
        deadline = DateTime.UtcNow.AddHours(1.0), 
        requestHeaders = Metadata(), 
        cancellationToken = CancellationToken.None, 
        peer = "127.0.0.1", 
        authContext = null,
        contextPropagationToken = null,
        writeHeadersFunc = (fun _ -> TaskUtils.CompletedTask),
        writeOptionsGetter = (fun () -> WriteOptions()),
        writeOptionsSetter = (fun _ -> ()))

let getLease leaseId asAt asOn =
    async {
        let asOfDate =
            Tutorial.Lease.V1.AsOfDate(
                AsAtTime = !@@asAt,
                AsOnDate = !@asOn)
        let req = Tutorial.Lease.V1.GetLeaseRequest(LeaseId = leaseId, AsOfDate = asOfDate)
        let! res = leaseAPI.GetLease(req, serverCtx) |> Async.AwaitTask
        return res.Lease
    }

let rec getLeaseEvents leaseId asAt asOn pageToken =
    asyncSeq {
        let asOfDate =
            Tutorial.Lease.V1.AsOfDate(
                AsAtTime = !@@asAt,
                AsOnDate = !@asOn)
        let req = 
            Tutorial.Lease.V1.ListLeaseEventsRequest(
                LeaseId = leaseId, 
                AsOfDate = asOfDate,
                PageSize = 5,
                PageToken = pageToken)
        let! res = leaseAPI.ListLeaseEvents(req, serverCtx) |> Async.AwaitTask
        match res.NextPageToken with
        | "" -> yield! res.Events |> AsyncSeq.ofSeq
        | token -> yield! getLeaseEvents leaseId asAt asOn token
    }

let deleteLeaseEvent leaseId eventId =
    async {
        let req = Tutorial.Lease.V1.DeleteLeaseEventRequest(LeaseId = leaseId, EventId = eventId)
        let! res = leaseAPI.DeleteLeaseEvent(req, serverCtx) |> Async.AwaitTask
        return res.Message
    }

let createLease lease =
    async {
        let req = Tutorial.Lease.V1.CreateLeaseRequest(Lease = lease)
        let! res = leaseAPI.CreateLease(req, serverCtx) |> Async.AwaitTask
        return res.Message
    }

let schedulePayment leaseId payment =
    async {
        let req = Tutorial.Lease.V1.SchedulePaymentRequest(LeaseId = leaseId, Payment = payment)
        let! res = leaseAPI.SchedulePayment(req, serverCtx) |> Async.AwaitTask
        return res.Message
    }

let receivePayment leaseId payment =
    async {
        let req = Tutorial.Lease.V1.ReceivePaymentRequest(LeaseId = leaseId, Payment = payment)
        let! res = leaseAPI.ReceivePayment(req, serverCtx) |> Async.AwaitTask
        return res.Message
    }

let terminateLease leaseId effDate =
    async {
        let req = Tutorial.Lease.V1.TerminateLeaseRequest(LeaseId = leaseId, EffectiveDate = !@effDate)
        let! res = leaseAPI.TerminateLease(req, serverCtx) |> Async.AwaitTask
        return res.Message
    }

let testCreateLease =
    testAsync "should successfully create lease" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let startDate = DateTime(2019, 1, 1)
        let lease =
            Tutorial.Lease.V1.Lease(
                LeaseId = leaseId,
                UserId = userId,
                StartDate = !@startDate,
                MaturityDate = !@DateTime(2019, 12, 31),
                MonthlyPaymentAmount = !!50.0m)
        let! actualRes = createLease lease
        let expectedRes = sprintf "successfully created lease-%s" leaseId
        actualRes |> Expect.equal "should equal expected response" expectedRes
        let! actualLeaseObs = getLease leaseId DateTime.UtcNow startDate
        let expectedLeaseObs =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        actualLeaseObs 
        |> Expect.equal "should equal expected lease" expectedLeaseObs
    }

let testSchedulePayment =
    testAsync "should successfully create lease and schedule a payment" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let s1Id = Guid.create()
        let startDate = DateTime(2019, 1, 1)
        let lease =
            Tutorial.Lease.V1.Lease(
                LeaseId = leaseId,
                UserId = userId,
                StartDate = !@startDate,
                MaturityDate = !@DateTime(2019, 12, 31),
                MonthlyPaymentAmount = !!50.0m)
        do! createLease lease |> Async.Ignore
        let s1Date = DateTime(2019, 2, 1)
        let scheduledPayment =
            Tutorial.Lease.V1.Payment(
                PaymentId = s1Id,
                PaymentDate = !@s1Date,
                PaymentAmount = !!50m)
        do! schedulePayment leaseId scheduledPayment |> Async.Ignore
        let! leaseObsAtStart = getLease leaseId DateTime.UtcNow startDate
        let! leaseObsAtS1 = getLease leaseId DateTime.UtcNow s1Date
        let expectedLeaseObsAtStart =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        let expectedLeaseObsAtS1 =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                TotalScheduled = !!50m,
                TotalPaid = !!0m,
                AmountDue = !!50m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        leaseObsAtStart 
        |> Expect.equal "should equal expected lease at start date" expectedLeaseObsAtStart
        leaseObsAtS1
        |> Expect.equal "should equal expected lease at s1 date" expectedLeaseObsAtS1
    }

let testReceivePayment =
    testAsync "should successfully create lease, schedule payment, and receive payment" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let s1Id = Guid.create()
        let p1Id = Guid.create()
        let startDate = DateTime(2019, 1, 1)
        let lease =
            Tutorial.Lease.V1.Lease(
                LeaseId = leaseId,
                UserId = userId,
                StartDate = !@startDate,
                MaturityDate = !@DateTime(2019, 12, 31),
                MonthlyPaymentAmount = !!50.0m)
        do! createLease lease |> Async.Ignore
        let s1Date = DateTime(2019, 2, 1)
        let scheduledPayment =
            Tutorial.Lease.V1.Payment(
                PaymentId = s1Id,
                PaymentDate = !@s1Date,
                PaymentAmount = !!50m)
        do! schedulePayment leaseId scheduledPayment |> Async.Ignore
        let p1Date = DateTime(2019, 2, 2)
        let receivedPayment =
            Tutorial.Lease.V1.Payment(
                PaymentId = p1Id,
                PaymentDate = !@p1Date,
                PaymentAmount = !!40m)
        do! receivePayment leaseId receivedPayment |> Async.Ignore 
        let! loanObsAtStart = getLease leaseId DateTime.UtcNow startDate
        let! loanObsAtS1 = getLease leaseId DateTime.UtcNow s1Date
        let! loanObsAtP1 = getLease leaseId DateTime.UtcNow p1Date
        let expectedLeaseObsAtStart =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtStart
        |> Expect.equal "should equal expected lease at start date" expectedLeaseObsAtStart
        let expectedLeaseObsAtS1 =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                TotalScheduled = !!50m,
                TotalPaid = !!0m,
                AmountDue = !!50m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtS1
        |> Expect.equal "should equal expected lease at s1 date" expectedLeaseObsAtS1
        let expectedLeaseObsAtP1 =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                TotalScheduled = !!50m,
                TotalPaid = !!40m,
                AmountDue = !!10m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtP1
        |> Expect.equal "should equal expected lease at p1 date" expectedLeaseObsAtP1
        let! p1Events = getLeaseEvents leaseId DateTime.UtcNow p1Date "" |> AsyncSeq.toListAsync
        let expectedP1Events =
            [ Tutorial.Lease.V1.LeaseEvent(
                EventId = 3,
                EventCreatedTime = !@@getUtcNow(),
                EventEffectiveDate = !@p1Date,
                EventType = "PaymentReceived") 
              Tutorial.Lease.V1.LeaseEvent(
                EventId = 2,
                EventCreatedTime = !@@getUtcNow(),
                EventEffectiveDate = !@s1Date,
                EventType = "PaymentScheduled")
              Tutorial.Lease.V1.LeaseEvent(
                EventId = 1,
                EventCreatedTime = !@@getUtcNow(),
                EventEffectiveDate = !@startDate,
                EventType = "LeaseCreated") ]
        p1Events
        |> Expect.sequenceEqual "should equal expected events at p1" expectedP1Events
        do! deleteLeaseEvent leaseId 3 |> Async.Ignore
        let! loanObsAtP1AfterDelete = getLease leaseId DateTime.UtcNow p1Date
        loanObsAtP1AfterDelete
        |> Expect.equal "should equal s1 lease at p1 date" expectedLeaseObsAtS1
        let! p1EventsAfterDelete = getLeaseEvents leaseId DateTime.UtcNow p1Date "" |> AsyncSeq.toListAsync
        let expectedP1EventsAfterDelete =
            [ Tutorial.Lease.V1.LeaseEvent(
                EventId = 2,
                EventCreatedTime = !@@getUtcNow(),
                EventEffectiveDate = !@s1Date,
                EventType = "PaymentScheduled") 
              Tutorial.Lease.V1.LeaseEvent(
                EventId = 1,
                EventCreatedTime = !@@getUtcNow(),
                EventEffectiveDate = !@startDate,
                EventType = "LeaseCreated") ]
        p1EventsAfterDelete
        |> Expect.sequenceEqual "should equal expected events at p1 after delete" expectedP1EventsAfterDelete
    }

let testTerminateLease = 
    testAsync "should successfully create lease and then terminate it" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let startDate = DateTime(2019, 1, 1)
        let beforeTeminationDate = DateTime(2019, 4, 29)
        let terminationDate = DateTime(2019, 4, 30)
        let afterTerminationDate = DateTime(2019, 5, 1)
        let lease =
            Tutorial.Lease.V1.Lease(
                LeaseId = leaseId,
                UserId = userId,
                StartDate = !@startDate,
                MaturityDate = !@DateTime(2019, 12, 31),
                MonthlyPaymentAmount = !!50.0m)
        do! createLease lease |> Async.Ignore
        do! terminateLease leaseId terminationDate |> Async.Ignore
        let expectedLeaseObsBeforeTermination =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        let expectedLeaseObsAfterTermination =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Terminated)
        let! actualLeaseBeforeTermination = getLease leaseId DateTime.UtcNow beforeTeminationDate
        let! actualLeaseAfterTermination = getLease leaseId DateTime.UtcNow afterTerminationDate
        actualLeaseBeforeTermination 
        |> Expect.equal "should equal expected lease before termination" expectedLeaseObsBeforeTermination
        actualLeaseAfterTermination
        |> Expect.equal "should equal expected lease after termination" expectedLeaseObsAfterTermination
    }


let tests =
    testList "test Lease" [
        testCreateLease
        testSchedulePayment
        testReceivePayment
        testTerminateLease
    ]

[<EntryPoint>]
let main argv =
    runTestsWithArgs defaultConfig argv tests
