module Lease.Api

open Lease.Implementation
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
    (service:Service)
    : Handle =
    fun (ctx:HttpContext) ->
        let { request = { rawForm = body }} = ctx
        asyncResult {
            let! newLease =
                body
                |> NewLeaseSchema.deserializeFromBytes
                |> Result.map NewLeaseSchema.toDomain
                |> AsyncResult.ofResult
            return! service.create newLease
        }

let handleGetLease 
    (service:Service)
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
            let! result =
                match (asOf, asAt) with
                | (None, None) -> service.get leaseId Latest
                | (Some asOfDate, None) -> service.get leaseId asOfDate
                | (None, Some asAtDate) -> service.get leaseId asAtDate
                | (Some _, Some _) -> "only specify asOf or asAt, not both" |> AsyncResult.ofError
            return result
        }

let handleModifyLease 
    (service:Service)
    (leaseIdParam: string) 
    : Handle =
    fun (ctx:HttpContext) ->
        let { request = { rawForm = body } as req } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption "could not parse leaseId"
                |> AsyncResult.ofResult
            let! newLease =
                body
                |> NewLeaseSchema.deserializeFromBytes
                |> Result.map NewLeaseSchema.toDomain
                |> AsyncResult.ofResult
            let lease =
                { LeaseId = leaseId
                  StartDate = newLease.StartDate
                  MaturityDate = newLease.MaturityDate
                  MonthlyPaymentAmount = newLease.MonthlyPaymentAmount }
            let effDate = 
                req.queryParam "effDate" 
                |> Option.ofChoice
                |> Option.bind DateTime.tryParse
                |> Option.map UMX.tag<effectiveDate>
                |> Option.defaultValue %newLease.StartDate 
            return! service.modify lease effDate
        }

let handleDeleteLease
    (service:Service)
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
            let effDate = 
                req.queryParam "effDate" 
                |> Option.ofChoice
                |> Option.bind DateTime.tryParse
                |> Option.map UMX.tag<effectiveDate>
                |> Option.defaultValue %DateTime.UtcNow
            return! service.terminate leaseId effDate
        }

let handleSchedulePayment
    (service:Service)
    (leaseIdParam: string) 
    : Handle =
    fun (ctx:HttpContext) ->
        let { request = { rawForm = body } } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption "could not parse leaseId"
                |> AsyncResult.ofResult
            let! payment =
                body
                |> PaymentSchema.deserializeFromBytes
                |> Result.map PaymentSchema.toDomain
                |> AsyncResult.ofResult
            return! service.schedulePayment leaseId payment
        }

let handleReceivePayment
    (service:Service)
    (leaseIdParam: string) 
    : Handle =
    fun (ctx:HttpContext) ->
        let { request = { rawForm = body } } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption "could not parse leaseId"
                |> AsyncResult.ofResult
            let! payment =
                body
                |> PaymentSchema.deserializeFromBytes
                |> Result.map PaymentSchema.toDomain
                |> AsyncResult.ofResult
            return! service.receivePayment leaseId payment
        }

let handleUndo
    (service:Service)
    (leaseIdParam: string) 
    (eventIdParam: string) 
    : Handle =
    fun (ctx:HttpContext) ->
        let { request = { rawForm = body } } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption "could not parse leaseId"
                |> AsyncResult.ofResult
            let! eventId = 
                eventIdParam 
                |> Int.tryParse
                |> Option.map UMX.tag<eventId>
                |> Result.ofOption "could not parse eventId"
                |> AsyncResult.ofResult
            return! service.undo leaseId eventId
        }
