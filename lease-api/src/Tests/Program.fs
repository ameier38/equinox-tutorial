open Expecto
open Expecto.Flip
open FSharp.UMX
open Google.Protobuf
open Grpc.Core
open Lease
open Lease.Operators
open System

let leaseApiHost = Some "localhost" |> Env.getEnv "LEASE_API_HOST"
let leaseApiPort = Some "50051" |> Env.getEnv "LEASE_API_PORT" |> int
let channelTarget = sprintf "%s:%d" leaseApiHost leaseApiPort

let leaseChannel = Channel(channelTarget, ChannelCredentials.Insecure)
let leaseAPIClient = Tutorial.Lease.V1.LeaseAPI.LeaseAPIClient(leaseChannel)

module DateTime =
    let toProtoDate (dt:DateTime) =
        Google.Type.Date.FromDateTime dt
    let toProtoTimestamp (dt:DateTime) =
        dt |> DateTime.toUtc |> WellKnownTypes.Timestamp.FromDateTime

module Guid =
    let create () = Guid.NewGuid() |> Guid.toStringN

let getLease leaseId asAt asOn =
    let asOf =
        Tutorial.Lease.V1.AsOf(
            AsAtTime = !@@asAt,
            AsOnDate = !@asOn)
    let req = Tutorial.Lease.V1.GetLeaseRequest(LeaseId = leaseId, AsOf = asOf)
    leaseAPIClient.GetLease(req).Lease

let getLeaseEvents leaseId asAt asOn =
    let rec recurse pageToken =
        seq {
            let asOf =
                Tutorial.Lease.V1.AsOf(
                    AsAtTime = !@@asAt,
                    AsOnDate = !@asOn)
            let req = 
                Tutorial.Lease.V1.ListLeaseEventsRequest(
                    LeaseId = leaseId, 
                    AsOf = asOf,
                    PageSize = 1,
                    PageToken = pageToken)
            let res = leaseAPIClient.ListLeaseEvents(req)
            yield! res.Events
            match res.NextPageToken with
            | "" -> ()
            | token -> yield! recurse token
        }
    recurse ""

let deleteLeaseEvent leaseId eventId =
    let req = Tutorial.Lease.V1.DeleteLeaseEventRequest(LeaseId = leaseId, EventId = eventId)
    leaseAPIClient.DeleteLeaseEvent(req).Message

let createLease lease =
    let req = Tutorial.Lease.V1.CreateLeaseRequest(Lease = lease)
    leaseAPIClient.CreateLease(req).Message

let schedulePayment leaseId payment =
    let req = Tutorial.Lease.V1.SchedulePaymentRequest(LeaseId = leaseId, ScheduledPayment = payment)
    leaseAPIClient.SchedulePayment(req).Message

let receivePayment leaseId payment =
    let req = Tutorial.Lease.V1.ReceivePaymentRequest(LeaseId = leaseId, ReceivedPayment = payment)
    leaseAPIClient.ReceivePayment(req).Message

let terminateLease leaseId termination =
    let req = Tutorial.Lease.V1.TerminateLeaseRequest(LeaseId = leaseId, Termination = termination)
    leaseAPIClient.TerminateLease(req).Message

let cleanLeaseObservation (obs:Tutorial.Lease.V1.LeaseObservation) =
    Tutorial.Lease.V1.LeaseObservation(
        UpdatedOnDate = obs.UpdatedOnDate,
        LeaseId = obs.LeaseId,
        UserId = obs.UserId,
        CommencementDate = obs.CommencementDate,
        ExpirationDate = obs.ExpirationDate,
        MonthlyPaymentAmount = obs.MonthlyPaymentAmount,
        TotalScheduled = obs.TotalScheduled,
        TotalPaid = obs.TotalPaid,
        AmountDue = obs.AmountDue,
        LeaseStatus = obs.LeaseStatus,
        TerminatedDate = obs.TerminatedDate)

let cleanLeaseEvent (event:Tutorial.Lease.V1.LeaseEvent) =
    Tutorial.Lease.V1.LeaseEvent(
        EventId = event.EventId,
        EventEffectiveDate = event.EventEffectiveDate,
        EventType = event.EventType) 

let testPagination =
    test "should successfully paginate" {
        let items = ["A"; "B"; "C"; "D"; "E"]
        let page1 = items |> Pagination.getPage %"" %2
        page1.Page
        |> Seq.toList
        |> Expect.equal "should equal first two elements" ["A"; "B"]
        %page1.NextPageToken
        |> Expect.equal "should equal encoded 2" ("index-2" |> String.toBase64)
        let page2 = items |> Pagination.getPage page1.NextPageToken %2
        page2.Page
        |> Seq.toList
        |> Expect.equal "should equal second two elements" ["C"; "D"]
        let page3 = items |> Pagination.getPage page2.NextPageToken %2
        page3.Page
        |> Seq.toList
        |> Expect.equal "should equal last element" ["E"]
        %page3.NextPageToken
        |> Expect.equal "should equal empty string" ""
    }

let testCreateLease =
    test "should successfully create lease" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let commencementDate = DateTime(2019, 1, 1)
        let expirationDate = DateTime(2019, 12, 31)
        let lease =
            Tutorial.Lease.V1.Lease(
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m)
        let actualRes = createLease lease
        let expectedRes = sprintf "successfully created Lease-%s" leaseId
        actualRes |> Expect.equal "should equal expected response" expectedRes
        let actualLeaseObs = 
            getLease leaseId DateTime.UtcNow commencementDate
            |> cleanLeaseObservation
        let expectedLeaseObs =
            Tutorial.Lease.V1.LeaseObservation(
                UpdatedOnDate = !@commencementDate,
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        actualLeaseObs 
        |> Expect.equal "should equal expected lease" expectedLeaseObs
    }

let testSchedulePayment =
    test "should successfully create lease and schedule a payment" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let s1Id = Guid.create()
        let commencementDate = DateTime(2019, 1, 1)
        let expirationDate = DateTime(2019, 12, 31)
        let lease =
            Tutorial.Lease.V1.Lease(
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m)
        do createLease lease |> ignore
        let s1Date = DateTime(2019, 2, 1)
        let scheduledPayment =
            Tutorial.Lease.V1.ScheduledPayment(
                PaymentId = s1Id,
                ScheduledDate = !@s1Date,
                ScheduledAmount = !!50m)
        do schedulePayment leaseId scheduledPayment |> ignore
        let leaseObsAtStart = 
            getLease leaseId DateTime.UtcNow commencementDate
            |> cleanLeaseObservation
        let leaseObsAtS1 = 
            getLease leaseId DateTime.UtcNow s1Date
            |> cleanLeaseObservation
        let expectedLeaseObsAtStart =
            Tutorial.Lease.V1.LeaseObservation(
                UpdatedOnDate = !@commencementDate,
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        let expectedLeaseObsAtS1 =
            Tutorial.Lease.V1.LeaseObservation(
                UpdatedOnDate = !@s1Date,
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m,
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
    test "should successfully create lease, schedule payment, and receive payment" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let s1Id = Guid.create()
        let p1Id = Guid.create()
        let commencementDate = DateTime(2019, 1, 1)
        let expirationDate = DateTime(2019, 12, 31)
        let lease =
            Tutorial.Lease.V1.Lease(
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m)
        do createLease lease |> ignore
        let s1Date = DateTime(2019, 2, 1)
        let scheduledPayment =
            Tutorial.Lease.V1.ScheduledPayment(
                PaymentId = s1Id,
                ScheduledDate = !@s1Date,
                ScheduledAmount = !!50m)
        do schedulePayment leaseId scheduledPayment |> ignore
        let p1Date = DateTime(2019, 2, 2)
        let receivedPayment =
            Tutorial.Lease.V1.ReceivedPayment(
                PaymentId = p1Id,
                ReceivedDate = !@p1Date,
                ReceivedAmount = !!40m)
        do receivePayment leaseId receivedPayment |> ignore 
        let loanObsAtStart = 
            getLease leaseId DateTime.UtcNow commencementDate
            |> cleanLeaseObservation
        let loanObsAtS1 = 
            getLease leaseId DateTime.UtcNow s1Date
            |> cleanLeaseObservation
        let loanObsAtP1 = 
            getLease leaseId DateTime.UtcNow p1Date
            |> cleanLeaseObservation
        let expectedLeaseObsAtStart =
            Tutorial.Lease.V1.LeaseObservation(
                UpdatedOnDate = !@commencementDate,
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtStart
        |> Expect.equal "should equal expected lease at start date" expectedLeaseObsAtStart
        let expectedLeaseObsAtS1 =
            Tutorial.Lease.V1.LeaseObservation(
                UpdatedOnDate = !@s1Date,
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m,
                TotalScheduled = !!50m,
                TotalPaid = !!0m,
                AmountDue = !!50m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtS1
        |> Expect.equal "should equal expected lease at s1 date" expectedLeaseObsAtS1
        let expectedLeaseObsAtP1 =
            Tutorial.Lease.V1.LeaseObservation(
                UpdatedOnDate = !@p1Date,
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m,
                TotalScheduled = !!50m,
                TotalPaid = !!40m,
                AmountDue = !!10m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        loanObsAtP1
        |> Expect.equal "should equal expected lease at p1 date" expectedLeaseObsAtP1
        let p1Events = 
            getLeaseEvents leaseId DateTime.UtcNow p1Date
            |> Seq.map cleanLeaseEvent
        let expectedP1Events =
            [ Tutorial.Lease.V1.LeaseEvent(
                EventId = 3,
                EventEffectiveDate = !@p1Date,
                EventType = "PaymentReceived")
              Tutorial.Lease.V1.LeaseEvent(
                EventId = 2,
                EventEffectiveDate = !@s1Date,
                EventType = "PaymentScheduled")
              Tutorial.Lease.V1.LeaseEvent(
                EventId = 1,
                EventEffectiveDate = !@commencementDate,
                EventType = "LeaseCreated") ]
        p1Events
        |> Expect.sequenceEqual "should equal expected events at p1" expectedP1Events
        do deleteLeaseEvent leaseId 3 |> ignore
        let p1EventsAfterDelete = 
            getLeaseEvents leaseId DateTime.UtcNow p1Date
            |> Seq.map cleanLeaseEvent
        let expectedP1EventsAfterDelete =
            [ Tutorial.Lease.V1.LeaseEvent(
                EventId = 2,
                EventEffectiveDate = !@s1Date,
                EventType = "PaymentScheduled") 
              Tutorial.Lease.V1.LeaseEvent(
                EventId = 1,
                EventEffectiveDate = !@commencementDate,
                EventType = "LeaseCreated") ]
        p1EventsAfterDelete
        |> Expect.sequenceEqual "should equal expected events at p1 after delete" expectedP1EventsAfterDelete
        let loanObsAtP1AfterDelete = getLease leaseId DateTime.UtcNow p1Date
        loanObsAtP1AfterDelete
        |> Expect.equal "should equal s1 lease at p1 date" expectedLeaseObsAtS1
    }

let testTerminateLease = 
    test "should successfully create lease and then terminate it" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let commmencementDate = DateTime(2019, 1, 1)
        let expirationDate = DateTime(2019, 12, 31)
        let beforeTeminationDate = DateTime(2019, 4, 29)
        let terminationDate = DateTime(2019, 4, 30)
        let afterTerminationDate = DateTime(2019, 5, 1)
        let lease =
            Tutorial.Lease.V1.Lease(
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commmencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m)
        do createLease lease |> ignore
        let termination =
            Tutorial.Lease.V1.Termination(
                TerminationDate = !@terminationDate,
                TerminationReason = "Test")
        do terminateLease leaseId termination |> ignore
        let expectedLeaseObsBeforeTermination =
            Tutorial.Lease.V1.LeaseObservation(
                UpdatedOnDate = !@commmencementDate,
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commmencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Outstanding)
        let expectedLeaseObsAfterTermination =
            Tutorial.Lease.V1.LeaseObservation(
                UpdatedOnDate = !@terminationDate,
                LeaseId = leaseId,
                UserId = userId,
                CommencementDate = !@commmencementDate,
                ExpirationDate = !@expirationDate,
                MonthlyPaymentAmount = !!50.0m,
                TotalScheduled = !!0m,
                TotalPaid = !!0m,
                AmountDue = !!0m,
                LeaseStatus = Tutorial.Lease.V1.LeaseStatus.Terminated,
                TerminatedDate = !@terminationDate)
        let actualLeaseBeforeTermination = 
            getLease leaseId DateTime.UtcNow beforeTeminationDate
            |> cleanLeaseObservation
        let actualLeaseAfterTermination = 
            getLease leaseId DateTime.UtcNow afterTerminationDate
            |> cleanLeaseObservation
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
