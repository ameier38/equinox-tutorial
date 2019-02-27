module Lease.Api

open Ouroboros
open FSharp.UMX
open System
open Suave
open Suave.Successful
open Suave.ServerErrors
open Suave.Operators

type Handle = HttpContext -> AsyncResult<string,string>

let JSON data =
    OK data 
    >=> Writers.setMimeType "application/json; charset=utf-8"

let createHandler (handle: Handle) : WebPart =
    fun (ctx:HttpContext) ->
        async {
            match! handle ctx with
            | Ok data -> 
                return! JSON data ctx
            | Error err ->
                let errStr = err.ToString()
                return! INTERNAL_ERROR errStr ctx
        }

let handleCreateLease
    (handler:Handler<LeaseId,LeaseCommand,LeaseEvent,LeaseState,Result<string,string>>)
    : Handle =
    fun (ctx:HttpContext) ->
        let { request = { rawForm = body }} = ctx
        asyncResult {
            let! newLease =
                body
                |> NewLeaseSchema.deserializeFromBytes
                |> Result.map NewLeaseSchema.toDomain
                |> AsyncResult.ofResult
            let leaseId = 
                Guid.NewGuid() 
                |> UMX.tag<leaseId>
            let lease =
                { LeaseId = leaseId
                  StartDate = newLease.StartDate
                  MaturityDate = newLease.MaturityDate
                  MonthlyPaymentAmount = newLease.MonthlyPaymentAmount }
            let command = Create lease
            do! handler.execute leaseId command
            return "Success"
        }

let handleGetLease 
    (aggregate:Aggregate<LeaseCommand,LeaseEvent,LeaseState>)
    (handler:Handler<LeaseId,LeaseCommand,LeaseEvent,LeaseState,Result<string,string>>)
    (leaseIdParam: string) 
    : Handle =
    fun (ctx:HttpContext) ->
        let { request = req } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption "could not parse leaseId"
                |> AsyncResult.ofResult
            let asOf = 
                req.queryParam "asOf" 
                |> Option.ofChoice
                |> Option.bind DateTime.tryParse
                |> Option.map AsOf
            let asAt = 
                req.queryParam "asAt" 
                |> Option.ofChoice
                |> Option.bind DateTime.tryParse
                |> Option.map AsAt
            let projection (obsDate:ObservationDate) ({ Events = events }) =
                let leaseState = aggregate.reconstitute obsDate events
                (leaseState, events)
                |> LeaseStateSchema.fromDomain
                |> Result.map LeaseStateSchema.serializeToJson
            let! result =
                match (asOf, asAt) with
                | (None, None) -> handler.query leaseId Latest projection
                | (Some asOfDate, None) -> handler.query leaseId asOfDate projection
                | (None, Some asAtDate) -> handler.query leaseId asAtDate projection
                | (Some _, Some _) -> "only specify asOf or asAt, not both" |> AsyncResult.ofError
            return! result |> AsyncResult.ofResult
        }
