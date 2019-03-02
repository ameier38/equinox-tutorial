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

let getRequest endpoint =
    let req = 
        { HttpRequest.empty with 
            rawPath = endpoint
            rawMethod = "GET" }
    { HttpContext.empty with request = req }

let postRequest endpoint body =
    let req = 
        { HttpRequest.empty with 
            rawPath = endpoint
            rawForm = UTF8.bytes body
            rawMethod = "POST" }
    { HttpContext.empty with request = req }

let putRequest endpoint body =
    let req = 
        { HttpRequest.empty with 
            rawPath = endpoint
            rawForm = UTF8.bytes body
            rawMethod = "PUT" }
    { HttpContext.empty with request = req }

let getLease (leaseId:LeaseId) (query:string) =
    let leaseIdStr = %leaseId |> Guid.toStringN
    sprintf "/lease/%s%s" leaseIdStr query
    |> getRequest
    |> api

let createLease newLease =
    newLease
    |> Dto.LeaseSchema.fromDomain
    |> Dto.LeaseSchema.serializeToJson
    |> postRequest "/lease"
    |> api

let modifyLease modifiedLease =
    let leaseId = modifiedLease.LeaseId |> LeaseId.toStringN
    modifiedLease
    |> Dto.ModifiedLeaseSchema.fromDomain
    |> Dto.ModifiedLeaseSchema.serializeToJson
    |> putRequest (sprintf "/lease/%s" leaseId)
    |> api

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
              StartDate = DateTime(2018, 1, 1)
              MaturityDate = DateTime(2018, 5, 31)
              MonthlyPaymentAmount = 25m }
        let modifiedLease =
            { LeaseId = leaseId
              StartDate = DateTime(2018, 1, 1)
              MaturityDate = DateTime(2018, 12, 31)
              MonthlyPaymentAmount = 20m }
        let! createResponse = createLease newLease |> Async.map expectSome
        createResponse |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let createdState = 
            match createResponse |> getState with
            | Outstanding leaseState -> leaseState
            | _ -> failwith "lease should be outstanding"
        createdState.Lease |> Expect.equal "lease should match created" newLease
        let! modifyResponse = modifyLease modifiedLease |> Async.map expectSome
        match modifyResponse |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match modified" modifiedLease
        | _ -> failwith "lease should be outstanding"
        let createdDate = createdState.CreatedDate.ToString("o")
        let! getResponse = sprintf "?asAt=%s" createdDate |> getLease leaseId |> Async.map expectSome
        match getResponse |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match created" newLease
        | _ -> failwith "lease should be outstanding"
    }

[<Tests>]
let testApi =
    testList "test Lease" [
        testCreate
        testModify
    ]
