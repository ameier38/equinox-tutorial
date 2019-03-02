module Tests.Lease

open Expecto
open Expecto.Flip
open FSharp.UMX
open Lease
open Suave
open Suave.Filters
open Suave.Successful
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
        createdState.Lease |> Expect.equal "create lease should match newLease" newLease
        let! modifyCtx = modifyLease modifiedLease |> Async.map expectSome
        match modifyCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "lease should match modified" modifiedLease
        | _ -> failwith "lease should be outstanding"
        let createdDate = createdState.CreatedDate.ToString("o")
        let asAtQuery = sprintf "asAt=%s" createdDate
        let! getCtx = getLease leaseId asAtQuery |> Async.map expectSome
        match getCtx |> getState with
        | Outstanding leaseState ->
            leaseState.Lease |> Expect.equal "get lease should match newLease" newLease
        | _ -> failwith "lease should be outstanding"
    }

[<Tests>]
let testApi =
    testList "test Lease" [
        testCreate
        testModify
    ]
