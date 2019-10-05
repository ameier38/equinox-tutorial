open Expecto
open Expecto.Flip
open FSharp.UMX
open Google.Protobuf
open Grpc.Core
open Grpc.Core.Testing
open Grpc.Core.Utils
open Lease
open Lease.Store
open Lease.Service
open Lease.Operators
open Newtonsoft.Json
open Serilog
open System
open System.Threading

let getUtcNow () = DateTime(2019, 10, 1)
let logger = LoggerConfiguration().WriteTo.Console().CreateLogger()
let config = Config.Load()
let store = Store(config)
let serializationSettings = JsonSerializerSettings()
let codec = FsCodec.NewtonsoftJson.Codec.Create<StoredEvent>(serializationSettings)
let leaseAPI = LeaseAPIImpl(getUtcNow, store, codec, logger)

module DateTime =
    let toProtoDate (dt:DateTime) =
        Google.Type.Date.FromDateTime dt
    let toProtoTimestamp (dt:DateTime) =
        dt |> DateTime.toUtc |> WellKnownTypes.Timestamp.FromDateTime

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

let getLeaseEvents leaseId asAt asOn =
    let rec recurse pageToken =
        seq {
            let asOfDate =
                Tutorial.Lease.V1.AsOfDate(
                    AsAtTime = !@@asAt,
                    AsOnDate = !@asOn)
            let req = 
                Tutorial.Lease.V1.ListLeaseEventsRequest(
                    LeaseId = leaseId, 
                    AsOfDate = asOfDate,
                    PageSize = 1,
                    PageToken = pageToken)
            let res = leaseAPI.ListLeaseEvents(req, serverCtx) |> Async.AwaitTask |> Async.RunSynchronously
            yield! res.Events
            match res.NextPageToken with
            | "" -> ()
            | token -> yield! recurse token
        }
    recurse ""

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
        let req = Tutorial.Lease.V1.SchedulePaymentRequest(LeaseId = leaseId, ScheduledPayment = payment)
        let! res = leaseAPI.SchedulePayment(req, serverCtx) |> Async.AwaitTask
        return res.Message
    }

let receivePayment leaseId payment =
    async {
        let req = Tutorial.Lease.V1.ReceivePaymentRequest(LeaseId = leaseId, ReceivedPayment = payment)
        let! res = leaseAPI.ReceivePayment(req, serverCtx) |> Async.AwaitTask
        return res.Message
    }

let terminateLease leaseId termination =
    async {
        let req = Tutorial.Lease.V1.TerminateLeaseRequest(LeaseId = leaseId, Termination = termination)
        let! res = leaseAPI.TerminateLease(req, serverCtx) |> Async.AwaitTask
        return res.Message
    }

let testPagination =
    test "should successfully paginate" {
        let items = ["A"; "B"; "C"; "D"]
        let page1 = items |> Pagination.getPage %"" %2
        page1.Page
        |> Seq.toList
        |> Expect.equal "should equal first two elements" ["A"; "B"]
        %page1.NextPageToken
        |> Expect.equal "should equal encoded 2" ("Index-2" |> String.toBase64)
        let page2 = items |> Pagination.getPage %"" %5
        page2.Page
        |> Seq.toList
        |> Expect.equal "should equal full list" items
        %page2.NextPageToken
        |> Expect.equal "should equal empty string" ""
    }

let testCreateLease =
    testAsync "should successfully create lease" {
        let now = getUtcNow()
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
        let expectedRes = sprintf "successfully created Lease-%s" leaseId
        actualRes |> Expect.equal "should equal expected response" expectedRes
        let! actualLeaseObs = getLease leaseId now startDate
        let expectedLeaseObs =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                CreatedTime = !@@now,
                UpdatedTime = !@@now,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        actualLeaseObs 
        |> Expect.equal "should equal expected lease" expectedLeaseObs
    }

let testSchedulePayment =
    testAsync "should successfully create lease and schedule a payment" {
        let now = getUtcNow()
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
            Tutorial.Lease.V1.ScheduledPayment(
                PaymentId = s1Id,
                ScheduledDate = !@s1Date,
                ScheduledAmount = !!50m)
        do! schedulePayment leaseId scheduledPayment |> Async.Ignore
        let! leaseObsAtStart = getLease leaseId now startDate
        let! leaseObsAtS1 = getLease leaseId now s1Date
        let expectedLeaseObsAtStart =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                CreatedTime = !@@now,
                UpdatedTime = !@@now,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        let expectedLeaseObsAtS1 =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                CreatedTime = !@@now,
                UpdatedTime = !@@now,
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
        let now = getUtcNow()
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
            Tutorial.Lease.V1.ScheduledPayment(
                PaymentId = s1Id,
                ScheduledDate = !@s1Date,
                ScheduledAmount = !!50m)
        do! schedulePayment leaseId scheduledPayment |> Async.Ignore
        let p1Date = DateTime(2019, 2, 2)
        let receivedPayment =
            Tutorial.Lease.V1.ReceivedPayment(
                PaymentId = p1Id,
                ReceivedDate = !@p1Date,
                ReceivedAmount = !!40m)
        do! receivePayment leaseId receivedPayment |> Async.Ignore 
        let! loanObsAtStart = getLease leaseId now startDate
        let! loanObsAtS1 = getLease leaseId now s1Date
        let! loanObsAtP1 = getLease leaseId now p1Date
        let expectedLeaseObsAtStart =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                CreatedTime = !@@now,
                UpdatedTime = !@@now,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtStart
        |> Expect.equal "should equal expected lease at start date" expectedLeaseObsAtStart
        let expectedLeaseObsAtS1 =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                CreatedTime = !@@now,
                UpdatedTime = !@@now,
                TotalScheduled = !!50m,
                TotalPaid = !!0m,
                AmountDue = !!50m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtS1
        |> Expect.equal "should equal expected lease at s1 date" expectedLeaseObsAtS1
        let expectedLeaseObsAtP1 =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                CreatedTime = !@@now,
                UpdatedTime = !@@now,
                TotalScheduled = !!50m,
                TotalPaid = !!40m,
                AmountDue = !!10m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtP1
        |> Expect.equal "should equal expected lease at p1 date" expectedLeaseObsAtP1
        let p1Events = 
            getLeaseEvents leaseId now p1Date
            |> Seq.map (fun e -> e.EventPayload <- ""; e)
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
        let p1EventsAfterDelete = 
            getLeaseEvents leaseId now p1Date
            |> Seq.map (fun e -> e.EventPayload <- ""; e)
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
        let! loanObsAtP1AfterDelete = getLease leaseId now p1Date
        loanObsAtP1AfterDelete
        |> Expect.equal "should equal s1 lease at p1 date" expectedLeaseObsAtS1
    }

let testTerminateLease = 
    testAsync "should successfully create lease and then terminate it" {
        let now = getUtcNow()
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
        let termination =
            Tutorial.Lease.V1.Termination(
                TerminationDate = !@terminationDate,
                TerminationReason = "Test")
        do! terminateLease leaseId termination |> Async.Ignore
        let expectedLeaseObsBeforeTermination =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                CreatedTime = !@@now,
                UpdatedTime = !@@now,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        let expectedLeaseObsAfterTermination =
            Tutorial.Lease.V1.LeaseObservation(
                Lease = lease,
                CreatedTime = !@@now,
                UpdatedTime = !@@now,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Terminated,
                TerminatedDate = !@terminationDate)
        let! actualLeaseBeforeTermination = getLease leaseId now beforeTeminationDate
        let! actualLeaseAfterTermination = getLease leaseId now afterTerminationDate
        actualLeaseBeforeTermination 
        |> Expect.equal "should equal expected lease before termination" expectedLeaseObsBeforeTermination
        actualLeaseAfterTermination
        |> Expect.equal "should equal expected lease after termination" expectedLeaseObsAfterTermination
    }

let tests =
    testList "test Lease" [
        testPagination
        testCreateLease
        testSchedulePayment
        testReceivePayment
        testTerminateLease
    ]

[<EntryPoint>]
let main argv =
    runTestsWithArgs defaultConfig argv tests
