module Tests.Api

open Expecto
open Expecto.Flip
open FSharp.UMX
open Lease
open Lease.Api
open Lease.Implementation
open Suave
open System

let config = EventStoreConfig.load()
let aggregate = Aggregate.init()
let resolver = Store.connect config aggregate "lease"
let service = Service.init aggregate resolver
let api' = api service

let testLeaseId = Guid.NewGuid() |> UMX.tag<leaseId>

let expectSome (ctxOpt:HttpContext option) =
    match ctxOpt with
    | Some ctx -> ctx
    | _ -> failwith "no response"

let replaceLeaseId = function
    | NonExistent -> failwith "lease does not exist"
    | Corrupt _ -> failwith "lease is corrupt"
    | Outstanding data -> 
        { data with Lease = { data.Lease with LeaseId = testLeaseId }}
        |> Outstanding
    | Terminated data -> 
        { data with Lease = { data.Lease with LeaseId = testLeaseId }}
        |> Terminated

let getState ({ response = { content = res }}) =
    match res with
    | Bytes bytes -> 
        bytes
        |> LeaseStateSchema.deserializeFromBytes
        |> LeaseStateSchema.toDomain
        |> replaceLeaseId
    | _ -> failwith "no state"

let getStatus { response = { status = { code = code } }} = code

let postRequest endpoint body =
    let req = 
        { HttpRequest.empty with 
            rawPath = endpoint
            rawForm = UTF8.bytes body
            rawMethod = "POST" }
    { HttpContext.empty with request = req }

let testCreate =
    testAsync "should successfully create lease" {
        let newLease =
            { StartDate = DateTime(2018, 1, 1)
              MaturityDate = DateTime(2018, 5, 31)
              MonthlyPaymentAmount = 25m }
        let! response =
            newLease
            |> NewLeaseSchema.fromDomain
            |> NewLeaseSchema.serializeToJson
            |> postRequest "/lease"
            |> api'
            |> Async.map expectSome
        response |> getStatus |> Expect.equal "should return 200" HTTP_200.code
        let actualState = response |> getState
        let expectedState =
            { Lease =
                { LeaseId = testLeaseId
                  StartDate = newLease.StartDate
                  MaturityDate = newLease.MaturityDate
                  MonthlyPaymentAmount = newLease.MonthlyPaymentAmount }
              TotalScheduled = 0m
              TotalPaid = 0m
              AmountDue = 0m }
            |> Outstanding
        actualState |> Expect.equal "state should match expected" expectedState
    }

[<Tests>]
let testApi =
    testList "test Lease.Api" [
        testCreate
    ]