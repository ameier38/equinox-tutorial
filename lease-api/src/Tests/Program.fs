open Expecto
open Expecto.Flip
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
open System.Text.RegularExpressions

let config = Config.load()
let logger = LoggerConfiguration().WriteTo.Console().CreateLogger()
let store = Store(config)
let leaseService = LeaseServiceImpl(store, logger)

let formatJson json =
    JValue.Parse(json).ToString(Formatting.Indented)

module Message =
    let toJson (message:IMessage) =
        let jsonFormatter = JsonFormatter(JsonFormatter.Settings(true))
        jsonFormatter.Format(message) |> formatJson

module Guid =
    let create () = Guid.NewGuid() |> Guid.toStringN

module ExecuteResponse =
    type ResponseCase = Proto.Lease.ExecuteResponse.ResponseOneofCase
    let expectOk (response:Proto.Lease.ExecuteResponse) =
        match response.ResponseCase with
        | ResponseCase.Ok -> ()
        | ResponseCase.Error -> failwith response.Error
        | other -> failwithf "received invalid response case: %A" other

module QueryStateResponse =
    type QueryStateResponseCase = Proto.Lease.QueryStateResponse.ResponseOneofCase
    type LeaseStateCase = Proto.Lease.QueryStateResponse.Types.LeaseState.StateOneofCase
    let expectOk (response:Proto.Lease.QueryStateResponse) =
        match response.ResponseCase with
        | QueryStateResponseCase.Ok -> 
            let state = response.Ok
            match state.StateCase with
            | LeaseStateCase.Observation -> 
                state.Observation |> Message.toJson
            | LeaseStateCase.None -> failwith "Nonexistent"
            | other -> failwithf "invalid LeaseStateCase %A" other 
        | QueryStateResponseCase.Error ->
            response.Error |> failwith
        | other -> failwithf "invalid QueryStateResponseCase %A" other

module QueryEventsResponse =
    type QueryEventsResponseCase = Proto.Lease.QueryEventsResponse.ResponseOneofCase
    let expectOk (response:Proto.Lease.QueryEventsResponse) =
        let createdDate = DateTime(2019, 4, 28) |> DateTime.toUtc |> DateTime.toTimestamp
        match response.ResponseCase with
        | QueryEventsResponseCase.Ok ->
            response.Ok 
            |> fun msg ->
                let newEvents = 
                    msg.Events 
                    |> Seq.map (fun e -> 
                        Proto.Lease.LeaseEvent(
                            EventId=e.EventId,
                            EventCreatedDate=createdDate,
                            EventEffectiveDate=e.EventEffectiveDate,
                            EventType=e.EventType))
                let newMsg = Proto.Lease.QueryEventsResponse.Types.LeaseEvents()
                newMsg.Events.AddRange(newEvents)
                newMsg
            |> Message.toJson
        | QueryEventsResponseCase.Error ->
            response.Error |> failwith
        | other -> failwithf "invalid QueryEventsResponseCase %A" other

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
            Proto.Lease.AsOfDate(
                AsAt = (asAt |> DateTime.toTimestamp),
                AsOn = (asOn |> DateTime.toTimestamp))
        let query = Proto.Lease.Query(LeaseId=leaseId, AsOfDate=asOfDate)
        let! response = leaseService.QueryState(query, serverCtx) |> Async.AwaitTask
        return response |> QueryStateResponse.expectOk
    }

let getLeaseEvents leaseId asAt asOn =
    async {
        let asOfDate =
            Proto.Lease.AsOfDate(
                AsAt = (asAt |> DateTime.toTimestamp),
                AsOn = (asOn |> DateTime.toTimestamp))
        let query = Proto.Lease.Query(LeaseId=leaseId, AsOfDate=asOfDate)
        let! response = leaseService.QueryEvents(query, serverCtx) |> Async.AwaitTask
        return response |> QueryEventsResponse.expectOk
    }

let deleteLeaseEvent leaseId eventId =
    async {
        let deleteCommand = Proto.Lease.Command(LeaseId=leaseId, DeleteEvent=eventId)
        let! response = leaseService.Execute(deleteCommand, serverCtx) |> Async.AwaitTask
        return response |> ExecuteResponse.expectOk
    }

let createLease leaseId effDate newLease =
    async {
        let effDateTs = effDate |> DateTime.toTimestamp
        let leaseCommand = Proto.Lease.LeaseCommand(EffectiveDate=effDateTs, CreateLease=newLease)
        let command = Proto.Lease.Command(LeaseId=leaseId, LeaseCommand=leaseCommand)
        let! response = leaseService.Execute(command, serverCtx) |> Async.AwaitTask
        return response |> ExecuteResponse.expectOk
    }

let schedulePayment leaseId effDate paymentAmount =
    async {
        let effDateTs = effDate |> DateTime.toTimestamp
        let leaseCommand = Proto.Lease.LeaseCommand(EffectiveDate=effDateTs, SchedulePayment=paymentAmount)
        let command = Proto.Lease.Command(LeaseId=leaseId, LeaseCommand=leaseCommand)
        let! response = leaseService.Execute(command, serverCtx) |> Async.AwaitTask
        return response |> ExecuteResponse.expectOk
    }

let receivePayment leaseId effDate paymentAmount =
    async {
        let effDateTs = effDate |> DateTime.toTimestamp
        let leaseCommand = Proto.Lease.LeaseCommand(EffectiveDate=effDateTs, ReceivePayment=paymentAmount)
        let command = Proto.Lease.Command(LeaseId=leaseId, LeaseCommand=leaseCommand)
        let! response = leaseService.Execute(command, serverCtx) |> Async.AwaitTask
        return response |> ExecuteResponse.expectOk
    }

let terminateLease leaseId effDate =
    async {
        let effDateTs = effDate |> DateTime.toTimestamp
        let leaseCommand = Proto.Lease.LeaseCommand(EffectiveDate=effDateTs, TerminateLease=true)
        let command = Proto.Lease.Command(LeaseId=leaseId, LeaseCommand=leaseCommand)
        let! response = leaseService.Execute(command, serverCtx) |> Async.AwaitTask
        return response |> ExecuteResponse.expectOk
    }

let testCreateLease =
    testAsync "should successfully create lease" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let newLease =
            Proto.Lease.NewLease(
                UserId = userId,
                MaturityDate = (DateTime(2019, 12, 31) |> DateTime.toUtc |> DateTime.toTimestamp),
                MonthlyPaymentAmount = 50.0f)
        let createdDate = DateTime(2019, 1, 1) |> DateTime.toUtc
        do! createLease leaseId createdDate newLease
        let! asOfCreatedResponse = getLease leaseId DateTime.UtcNow createdDate
        let expectedAsOfCreatedResponse =
            sprintf
                """
                {
                  "observationDate": "2019-01-01T00:00:00Z",
                  "leaseId": "%s",
                  "userId": "%s",
                  "startDate": "2019-01-01T00:00:00Z",
                  "maturityDate": "2019-12-31T00:00:00Z",
                  "monthlyPaymentAmount": 50,
                  "totalScheduled": 0,
                  "totalPaid": 0,
                  "amountDue": 0,
                  "leaseStatus": "outstanding"
                }
                """
                <| leaseId
                <| userId
            |> formatJson
        asOfCreatedResponse 
        |> Expect.equal "should equal expectedAsOfCreatedResponse" expectedAsOfCreatedResponse
    }

let testSchedulePayment =
    testAsync "should successfully create lease and schedule payment" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let newLease =
            Proto.Lease.NewLease(
                UserId = userId,
                MaturityDate = (DateTime(2019, 12, 31) |> DateTime.toUtc |> DateTime.toTimestamp),
                MonthlyPaymentAmount = 50.0f)
        let createdDate = DateTime(2019, 1, 1) |> DateTime.toUtc
        do! createLease leaseId createdDate newLease
        let paymentDate = DateTime(2019, 2, 1) |> DateTime.toUtc
        do! schedulePayment leaseId paymentDate 50.0f
        let! asOfCreatedResponse = getLease leaseId DateTime.UtcNow createdDate
        let! asOfScheduledResponse = getLease leaseId DateTime.UtcNow paymentDate
        let expectedAsOfCreatedResponse =
            sprintf
                """
                {
                  "observationDate": "2019-01-01T00:00:00Z",
                  "leaseId": "%s",
                  "userId": "%s",
                  "startDate": "2019-01-01T00:00:00Z",
                  "maturityDate": "2019-12-31T00:00:00Z",
                  "monthlyPaymentAmount": 50,
                  "totalScheduled": 0,
                  "totalPaid": 0,
                  "amountDue": 0,
                  "leaseStatus": "outstanding"
                }
                """
                <| leaseId
                <| userId
            |> formatJson
        asOfCreatedResponse 
        |> Expect.equal "should equal expectedAsOfCreatedResponse" expectedAsOfCreatedResponse
        let expectedAsOfScheduledResponse =
            sprintf
                """
                {
                  "observationDate": "2019-02-01T00:00:00Z",
                  "leaseId": "%s",
                  "userId": "%s",
                  "startDate": "2019-01-01T00:00:00Z",
                  "maturityDate": "2019-12-31T00:00:00Z",
                  "monthlyPaymentAmount": 50,
                  "totalScheduled": 50,
                  "totalPaid": 0,
                  "amountDue": 50,
                  "leaseStatus": "outstanding"
                }
                """
                <| leaseId
                <| userId
            |> formatJson
        asOfScheduledResponse 
        |> Expect.equal "should equal expectedAsOfScheduledResponse" expectedAsOfScheduledResponse
    }

let testReceivePayment =
    testAsync "should successfully create lease, schedule payment, and receive payment" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let newLease =
            Proto.Lease.NewLease(
                UserId = userId,
                MaturityDate = (DateTime(2019, 12, 31) |> DateTime.toUtc |> DateTime.toTimestamp),
                MonthlyPaymentAmount = 50.0f)
        let createdDate = DateTime(2019, 1, 1) |> DateTime.toUtc
        do! createLease leaseId createdDate newLease
        let scheduledPaymentDate = DateTime(2019, 2, 1) |> DateTime.toUtc
        do! schedulePayment leaseId scheduledPaymentDate 50.0f
        let receivedPaymentDate = DateTime(2019, 2, 2) |> DateTime.toUtc
        do! receivePayment leaseId receivedPaymentDate 50.0f
        let! asOfReceivedResponse = getLease leaseId DateTime.UtcNow receivedPaymentDate
        let! leaseEventsResponse = getLeaseEvents leaseId DateTime.UtcNow receivedPaymentDate
        let expectedAsOfReceivedResponse =
            sprintf
                """
                {
                  "observationDate": "2019-02-02T00:00:00Z",
                  "leaseId": "%s",
                  "userId": "%s",
                  "startDate": "2019-01-01T00:00:00Z",
                  "maturityDate": "2019-12-31T00:00:00Z",
                  "monthlyPaymentAmount": 50,
                  "totalScheduled": 50,
                  "totalPaid": 50,
                  "amountDue": 0,
                  "leaseStatus": "outstanding"
                }
                """
                <| leaseId
                <| userId
            |> formatJson
        asOfReceivedResponse 
        |> Expect.equal "should equal expectedAsOfReceivedResponse" expectedAsOfReceivedResponse
        let expectedLeaseEventsResponse = 
            """
            {
              "events": [
                {
                  "eventId": 3,
                  "eventCreatedDate": "2019-04-28T00:00:00Z",
                  "eventEffectiveDate": "2019-02-02T00:00:00Z",
                  "eventType": "PaymentReceived"
                },
                {
                  "eventId": 2,
                  "eventCreatedDate": "2019-04-28T00:00:00Z",
                  "eventEffectiveDate": "2019-02-01T00:00:00Z",
                  "eventType": "PaymentScheduled"
                },
                {
                  "eventId": 1,
                  "eventCreatedDate": "2019-04-28T00:00:00Z",
                  "eventEffectiveDate": "2019-01-01T00:00:00Z",
                  "eventType": "LeaseCreated"
                }
              ]
            }
            """
            |> formatJson
        leaseEventsResponse
        |> Expect.equal "should equal expectedLeaseEventsResponse" expectedLeaseEventsResponse
    }

let testTerminateLease =
    testAsync "should successfully create and terminate lease" {
        let leaseId = Guid.create()
        let userId = Guid.create()
        let newLease =
            Proto.Lease.NewLease(
                UserId = userId,
                MaturityDate = (DateTime(2019, 12, 31) |> DateTime.toUtc |> DateTime.toTimestamp),
                MonthlyPaymentAmount = 50.0f)
        let createdDate = DateTime(2019, 1, 1) |> DateTime.toUtc
        do! createLease leaseId createdDate newLease
        let terminatedDate = DateTime(2019, 6, 30) |> DateTime.toUtc
        do! terminateLease leaseId terminatedDate
        let beforeDelete = DateTime.UtcNow
        do! deleteLeaseEvent leaseId 2
        let afterDelete = DateTime.UtcNow
        let! beforeDeleteResponse = getLease leaseId beforeDelete terminatedDate
        let! beforeDeleteEvents = getLeaseEvents leaseId beforeDelete terminatedDate
        let! afterDeleteResponse = getLease leaseId afterDelete terminatedDate
        let! afterDeleteEvents = getLeaseEvents leaseId afterDelete terminatedDate
        let expectedBeforeDeleteResponse =
            sprintf
                """
                {
                  "observationDate": "2019-06-30T00:00:00Z",
                  "leaseId": "%s",
                  "userId": "%s",
                  "startDate": "2019-01-01T00:00:00Z",
                  "maturityDate": "2019-12-31T00:00:00Z",
                  "monthlyPaymentAmount": 50,
                  "totalScheduled": 0,
                  "totalPaid": 0,
                  "amountDue": 0,
                  "leaseStatus": "terminated"
                }
                """
                <| leaseId
                <| userId
            |> formatJson
        beforeDeleteResponse 
        |> Expect.equal "should equal beforeDeleteResponse" expectedBeforeDeleteResponse
        let expectedBeforeDeleteEvents =
            """
            {
              "events": [
                {
                  "eventId": 2,
                  "eventCreatedDate": "2019-04-28T00:00:00Z",
                  "eventEffectiveDate": "2019-06-30T00:00:00Z",
                  "eventType": "LeaseTerminated"
                },
                {
                  "eventId": 1,
                  "eventCreatedDate": "2019-04-28T00:00:00Z",
                  "eventEffectiveDate": "2019-01-01T00:00:00Z",
                  "eventType": "LeaseCreated"
                }
              ]
            }
            """
            |> formatJson
        beforeDeleteEvents
        |> Expect.equal "should equal expectedBeforeDeleteEvents" expectedBeforeDeleteEvents
        let expectedAfterDeleteResponse =
            sprintf
                """
                {
                  "observationDate": "2019-06-30T00:00:00Z",
                  "leaseId": "%s",
                  "userId": "%s",
                  "startDate": "2019-01-01T00:00:00Z",
                  "maturityDate": "2019-12-31T00:00:00Z",
                  "monthlyPaymentAmount": 50,
                  "totalScheduled": 0,
                  "totalPaid": 0,
                  "amountDue": 0,
                  "leaseStatus": "outstanding"
                }
                """
                <| leaseId
                <| userId
            |> formatJson
        afterDeleteResponse
        |> Expect.equal "should equal afterDeleteResponse" expectedAfterDeleteResponse
        let expectedAfterDeleteEvents =
            """
            {
              "events": [
                {
                  "eventId": 1,
                  "eventCreatedDate": "2019-04-28T00:00:00Z",
                  "eventEffectiveDate": "2019-01-01T00:00:00Z",
                  "eventType": "LeaseCreated"
                }
              ]
            }
            """
            |> formatJson
        afterDeleteEvents
        |> Expect.equal "should equal expectedAfterDeleteEvents" expectedAfterDeleteEvents
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
