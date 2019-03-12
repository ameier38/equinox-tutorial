module Tests.Lease

open Expecto
open Expecto.Flip
open FSharp.UMX
open Lease
open Lease.Aggregate
open Lease.Store
open Lease.Service
open Serilog
open Suave
open System

let config = EventStoreConfig.load()
let logger = LoggerConfiguration().WriteTo.Console().CreateLogger()
let aggregate = Aggregate()
let store = Store(config, aggregate)
let service = Service(aggregate, store, logger)
let api = Api.init service

let makeLeaseId () = Guid.NewGuid() |> UMX.tag<leaseId>

let makeLease leaseId =
    { LeaseId = leaseId
      StartDate = DateTime(2018, 1, 1)
      MaturityDate = DateTime(2018, 12, 31)
      MonthlyPaymentAmount = 10m<monthlyPaymentAmount> }

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

let createLease lease =
    let body =
        lease
        |> Dto.LeaseSchema.fromDomain
        |> Dto.LeaseSchema.serializeToJson
    let req = makeRequest "/lease" "POST" "" body
    api req

let schedulePayment (leaseId:LeaseId) (payment:ScheduledPayment) =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    let endpoint = sprintf "/lease/%s/schedule" leaseIdStr
    let body =
        payment
        |> Dto.PaymentSchema.fromScheduledPayment
        |> Dto.PaymentSchema.serializeToJson
    let req = makeRequest endpoint "POST" "" body
    api req

let receivePayment (leaseId:LeaseId) (payment:Payment) =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    let endpoint = sprintf "/lease/%s/payment" leaseIdStr
    let body =
        payment
        |> Dto.PaymentSchema.fromPayment
        |> Dto.PaymentSchema.serializeToJson
    let req = makeRequest endpoint "POST" "" body
    api req

let terminate (leaseId:LeaseId) =
    let leaseIdStr = leaseId |> LeaseId.toStringN
    let endpoint = sprintf "/lease/%s" leaseIdStr
    let req = makeRequest endpoint "DELETE" "" ""
    api req

let testCreate =
    testAsync "should successfully create lease" {
        let leaseId = makeLeaseId ()
        let newLease = makeLease leaseId
        let! response = createLease newLease |> Async.map expectSome
        response |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        match response |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match expected" newLease
            leaseState.TotalPaid |> Expect.equal "total paid should be zero" 0m<paymentAmount>
            leaseState.TotalScheduled |> Expect.equal "total scheduled should be zero" 120m<scheduledPaymentAmount>
            leaseState.AmountDue |> Expect.equal "amount due should be zero" 120m
        | _ -> failwith "lease state should be Outstanding"
    }

let testSchedulePayment =
    testAsync "should successfully create a lease and schedule another payment" {
        let leaseId = makeLeaseId ()
        let newLease = makeLease leaseId
        let! createCtx = createLease newLease |> Async.map expectSome
        createCtx |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createCtx |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "lease should match new lease" newLease
        let payment =
            { ScheduledPaymentDate = DateTime(2019, 1, 31).ToUniversalTime()
              ScheduledPaymentAmount = 10m<scheduledPaymentAmount> }
        let! scheduleCtx = schedulePayment newLease.LeaseId payment |> Async.map expectSome
        match scheduleCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal payment amount" 130m<scheduledPaymentAmount>
            leaseState.TotalPaid |> Expect.equal "total paid should equal zero" 0m<paymentAmount>
            leaseState.AmountDue |> Expect.equal "amount due should equal payment amount" 130m
        | _ -> failwith "lease should be outstanding"
    }

let testReceivePayment =
    testAsync "should successfully create a lease and receive a payment" {
        let leaseId = makeLeaseId ()
        let newLease = makeLease leaseId
        let! createCtx = createLease newLease |> Async.map expectSome
        createCtx |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createCtx |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "lease should match new lease" newLease
        let receivedPayment =
            { PaymentDate = DateTime(2018, 1, 31).ToUniversalTime()
              PaymentAmount = 10m<paymentAmount> }
        let! receiveCtx = receivePayment newLease.LeaseId receivedPayment |> Async.map expectSome
        match receiveCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal scheduled payment amount" 120m<scheduledPaymentAmount>
            leaseState.TotalPaid |> Expect.equal "total paid should equal received payment amount" receivedPayment.PaymentAmount
            leaseState.AmountDue |> Expect.equal "amount due should equal zero" 110m
        | _ -> failwith "lease should be outstanding"
    }

let testTerminate =
    testAsync "should successfully create a lease and then terminate it" {
        let leaseId = makeLeaseId ()
        let newLease = makeLease leaseId
        let! createCtx = createLease newLease |> Async.map expectSome
        createCtx |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createCtx |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "lease should match new lease" newLease
        let! terminateCtx = terminate newLease.LeaseId |> Async.map expectSome
        match terminateCtx |> getState with
        | Terminated leaseState ->
            leaseState.Lease |> Expect.equal "lease should match new lease" newLease
            leaseState.TotalScheduled |> Expect.equal "total scheduled should equal scheduled payment amount" 120m<scheduledPaymentAmount>
            leaseState.TotalPaid |> Expect.equal "total paid should equal zero" 0m<paymentAmount>
            leaseState.AmountDue |> Expect.equal "amount due should equal scheduled payment amount" 120m
        | _ -> failwith "lease should be terminated"
    }

[<Tests>]
let testApi =
    testList "test Lease" [
        testCreate
        testSchedulePayment
        testReceivePayment
        testTerminate
    ]
