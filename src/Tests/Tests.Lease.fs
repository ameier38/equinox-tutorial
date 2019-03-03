module Tests.Lease

open Expecto
open Expecto.Flip
open FSharp.UMX
open Lease
open Suave
open System

let config = EventStoreConfig.load()
let aggregate = Aggregate.leaseAggregate
let resolver = Store.connect config aggregate
let service = Service.init aggregate resolver
let api = Api.init service

let createLeaseId () = Guid.NewGuid() |> UMX.tag<leaseId>

let expectSome (ctxOpt:HttpContext option) =
    match ctxOpt with
    | Some ctx -> ctx
    | _ -> failwith "no response"

let getState ({ response = { content = res }}) =
    match res with
    | Bytes bytes -> 
        bytes
        |> Dto.LeaseStateSchema.deserializeFromBytes
        |> Dto.LeaseStateSchema.toDomain
        |> Result.bimap id (fun err -> failwith err)
    | _ -> failwith "no state"

let getStatus { response = { status = { code = code } }} = code

let makeRequest endpoint method query body =
    let req = 
        { HttpRequest.empty with 
            rawPath = endpoint
            rawQuery = query
            rawMethod = method
            rawForm = body |> UTF8.bytes }
    { HttpContext.empty with request = req }

let getLease (leaseId:LeaseId) (query:string) =
    let endpoint = 
        %leaseId |> Guid.toStringN
        |> sprintf "/lease/%s"
    let req = makeRequest endpoint "GET" query ""
    api req

let createLease newLease =
    let body =
        newLease
        |> Dto.LeaseSchema.fromDomain
        |> Dto.LeaseSchema.serializeToJson
    let req = makeRequest "/lease" "POST" "" body
    api req

let modifyLease modifiedLease =
    let leaseId = modifiedLease.LeaseId |> LeaseId.toStringN
    let endpoint = sprintf "/lease/%s" leaseId
    let body =
        modifiedLease
        |> Dto.ModifiedLeaseSchema.fromDomain
        |> Dto.ModifiedLeaseSchema.serializeToJson
    let req = makeRequest endpoint "PUT" "" body
    api req

let schedulePayment (leaseId:LeaseId) (payment:Payment) =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    let endpoint = sprintf "/lease/%s/schedule" leaseIdStr
    let body =
        payment
        |> Dto.PaymentSchema.fromDomain
        |> Dto.PaymentSchema.serializeToJson
    let req = makeRequest endpoint "POST" "" body
    api req

let receivePayment (leaseId:LeaseId) (payment:Payment) =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    let endpoint = sprintf "/lease/%s/payment" leaseIdStr
    let body =
        payment
        |> Dto.PaymentSchema.fromDomain
        |> Dto.PaymentSchema.serializeToJson
    let req = makeRequest endpoint "POST" "" body
    api req

let terminate (leaseId:LeaseId) =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    let endpoint = sprintf "/lease/%s" leaseIdStr
    let req = makeRequest endpoint "DELETE" "" ""
    api req

let undo (leaseId:LeaseId) (eventId:EventId) =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    let endpoint = sprintf "/lease/%s/%d" leaseIdStr eventId
    let req = makeRequest endpoint "DELETE" "" ""
    api req

let testCreate =
    testAsync "should successfully create lease" {
        let newLease =
            { LeaseId = createLeaseId ()
              StartDate = DateTime(2018, 1, 1)
              MaturityDate = DateTime(2018, 5, 31)
              MonthlyPaymentAmount = 25m }
        let! response = createLease newLease |> Async.map expectSome
        response |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        match response |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match expected" newLease
            leaseState.TotalPaid |> Expect.equal "total paid should be zero" 0m
            leaseState.TotalScheduled |> Expect.equal "total scheduled should be zero" 0m
            leaseState.AmountDue |> Expect.equal "amount due should be zero" 0m
        | _ -> failwith "lease state should be Outstanding"
    }

let testModify =
    testAsync "should successfully create then modify a lease" {
        let leaseId = createLeaseId ()
        let newLease =
            { LeaseId = leaseId
              StartDate = DateTime(2018, 1, 1).ToUniversalTime()
              MaturityDate = DateTime(2018, 5, 31).ToUniversalTime()
              MonthlyPaymentAmount = 25m }
        let modifiedLease =
            { LeaseId = leaseId
              StartDate = DateTime(2018, 1, 1).ToUniversalTime()
              MaturityDate = DateTime(2018, 12, 31).ToUniversalTime()
              MonthlyPaymentAmount = 20m }
        let! createCtx = createLease newLease |> Async.map expectSome
        createCtx |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createCtx |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "create lease should match new lease" newLease
        let! modifyCtx = modifyLease modifiedLease |> Async.map expectSome
        match modifyCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match modified lease" modifiedLease
        | _ -> failwith "lease should be outstanding"
        let createdDate = createdState.CreatedDate.ToString("o")
        let asAtQuery = sprintf "asAt=%s" createdDate
        let! getCtx = getLease leaseId asAtQuery |> Async.map expectSome
        match getCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "get lease should match new lease" newLease
        | _ -> failwith "lease should be outstanding"
    }

let testSchedulePayment =
    testAsync "should successfully create a lease and schedule a payment" {
        let leaseId = createLeaseId ()
        let newLease =
            { LeaseId = leaseId
              StartDate = DateTime(2018, 1, 1).ToUniversalTime()
              MaturityDate = DateTime(2018, 5, 31).ToUniversalTime()
              MonthlyPaymentAmount = 25m }
        let! createCtx = createLease newLease |> Async.map expectSome
        createCtx |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createCtx |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "lease should match new lease" newLease
        let payment =
            { PaymentDate = DateTime(2018, 1, 2).ToUniversalTime()
              PaymentAmount = 25m }
        let! scheduleCtx = schedulePayment leaseId payment |> Async.map expectSome
        match scheduleCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal payment amount" payment.PaymentAmount
            leaseState.TotalPaid |> Expect.equal "total paid should equal zero" 0m
            leaseState.AmountDue |> Expect.equal "amount due should equal payment amount" payment.PaymentAmount
        | _ -> failwith "lease should be outstanding"
    }

let testReceivePayment =
    testAsync "should successfully create a lease, schedule a payment, and receive a payment" {
        let leaseId = createLeaseId ()
        let newLease =
            { LeaseId = leaseId
              StartDate = DateTime(2018, 1, 1).ToUniversalTime()
              MaturityDate = DateTime(2018, 5, 31).ToUniversalTime()
              MonthlyPaymentAmount = 25m }
        let! createCtx = createLease newLease |> Async.map expectSome
        createCtx |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createCtx |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "lease should match new lease" newLease
        let scheduledPayment =
            { PaymentDate = DateTime(2018, 1, 2).ToUniversalTime()
              PaymentAmount = 25m }
        let! scheduleCtx = schedulePayment leaseId scheduledPayment |> Async.map expectSome
        match scheduleCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal payment amount" scheduledPayment.PaymentAmount
            leaseState.TotalPaid |> Expect.equal "total paid should equal zero" 0m
            leaseState.AmountDue |> Expect.equal "amount due should equal payment amount" scheduledPayment.PaymentAmount
        | _ -> failwith "lease should be outstanding"
        let receivedPayment =
            { PaymentDate = DateTime(2018, 1, 3).ToUniversalTime()
              PaymentAmount = 25m }
        let! receiveCtx = receivePayment leaseId receivedPayment |> Async.map expectSome
        match receiveCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal scheduled payment amount" scheduledPayment.PaymentAmount
            leaseState.TotalPaid |> Expect.equal "total paid should equal received payment amount" receivedPayment.PaymentAmount
            leaseState.AmountDue |> Expect.equal "amount due should equal zero" 0m
        | _ -> failwith "lease should be outstanding"
    }

let testTerminate =
    testAsync "should successfully create a lease, schedule a payment, and terminate" {
        let leaseId = createLeaseId ()
        let newLease =
            { LeaseId = leaseId
              StartDate = DateTime(2018, 1, 1).ToUniversalTime()
              MaturityDate = DateTime(2018, 5, 31).ToUniversalTime()
              MonthlyPaymentAmount = 25m }
        let! createCtx = createLease newLease |> Async.map expectSome
        createCtx |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createCtx |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "lease should match new lease" newLease
        let scheduledPayment =
            { PaymentDate = DateTime(2018, 1, 2).ToUniversalTime()
              PaymentAmount = 25m }
        let! scheduleCtx = schedulePayment leaseId scheduledPayment |> Async.map expectSome
        match scheduleCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal payment amount" scheduledPayment.PaymentAmount
            leaseState.TotalPaid |> Expect.equal "total paid should equal zero" 0m
            leaseState.AmountDue |> Expect.equal "amount due should equal payment amount" scheduledPayment.PaymentAmount
        | _ -> failwith "lease should be outstanding"
        let! terminateCtx = terminate leaseId |> Async.map expectSome
        match terminateCtx |> getState with
        | Terminated leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal scheduled payment amount" scheduledPayment.PaymentAmount
            leaseState.TotalPaid |> Expect.equal "total paid should equal zero" 0m
            leaseState.AmountDue |> Expect.equal "amount due should equal scheduled payment amount" scheduledPayment.PaymentAmount
        | _ -> failwith "lease should be terminated"
    }

let testUndo =
    testAsync "should successfully create a lease, schedule a payment, receive a payment, and undo a payment" {
        let leaseId = createLeaseId ()
        let newLease =
            { LeaseId = leaseId
              StartDate = DateTime(2018, 1, 1).ToUniversalTime()
              MaturityDate = DateTime(2018, 5, 31).ToUniversalTime()
              MonthlyPaymentAmount = 25m }
        let! createCtx = createLease newLease |> Async.map expectSome
        createCtx |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createCtx |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "lease should match new lease" newLease
        let scheduledPayment =
            { PaymentDate = DateTime(2018, 1, 2).ToUniversalTime()
              PaymentAmount = 25m }
        let! scheduleCtx = schedulePayment leaseId scheduledPayment |> Async.map expectSome
        match scheduleCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal payment amount" scheduledPayment.PaymentAmount
            leaseState.TotalPaid |> Expect.equal "total paid should equal zero" 0m
            leaseState.AmountDue |> Expect.equal "amount due should equal payment amount" scheduledPayment.PaymentAmount
        | _ -> failwith "lease should be outstanding"
        let receivedPayment =
            { PaymentDate = DateTime(2018, 1, 3).ToUniversalTime()
              PaymentAmount = 25m }
        let! receiveCtx = receivePayment leaseId receivedPayment |> Async.map expectSome
        match receiveCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal scheduled payment amount" scheduledPayment.PaymentAmount
            leaseState.TotalPaid |> Expect.equal "total paid should equal received payment amount" receivedPayment.PaymentAmount
            leaseState.AmountDue |> Expect.equal "amount due should equal zero" 0m
        | _ -> failwith "lease should be outstanding"
        let! undoCtx = undo leaseId %2 |> Async.map expectSome
        match undoCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal scheduled payment amount" scheduledPayment.PaymentAmount
            leaseState.TotalPaid |> Expect.equal "total paid should equal zero" 0m
            leaseState.AmountDue |> Expect.equal "amount due should equal scheduled payment amount" scheduledPayment.PaymentAmount
        | _ -> failwith "lease should be outstanding"
    }

[<Tests>]
let testApi =
    testList "test Lease" [
        testCreate
        testModify
        testSchedulePayment
        testReceivePayment
        testTerminate
        testUndo
    ]
