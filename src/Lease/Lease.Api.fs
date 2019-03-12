module Lease.Api

open Dto
open FSharp.UMX
open System
open Suave
open Suave.Operators
open Suave.RequestErrors
open Suave.Filters

type Handle = HttpContext -> AsyncResult<string,string>
type HandlePath<'PathParams> = 'PathParams -> Handle

let ok s =
    fun (ctx:HttpContext) -> 
        { ctx with 
            response = 
                { ctx.response with 
                    status = HTTP_200.status
                    content = UTF8.bytes s |> Bytes }}

let JSON = Writers.setMimeType "application/json; charset=utf-8"

let createHandler 
    (handle:Handle) =
    fun (ctx:HttpContext) ->
        let onSuccess data = ok data ctx |> Some
        let onFailure err = failwith err
        handle ctx |> AsyncResult.bimap onSuccess onFailure

let createPathHandler 
    (handlePath:HandlePath<'PathParams>) = 
    fun (pathParams:'PathParams) ->
        handlePath pathParams
        |> createHandler

let getLeaseStateResponse
    (service:Service) =
    fun (leaseId:LeaseId) (obsDate:ObservationDate) ->
        async {
            let! state = service.query leaseId obsDate
            return!
                state
                |> LeaseStateSchema.fromDomain
                |> Result.map LeaseStateSchema.serializeToJson
                |> AsyncResult.ofResult
        }

let handleGetLease 
    (service:Service)
    : HandlePath<string> =
    fun leaseIdParam (ctx:HttpContext) ->
        let { request = req } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption (sprintf "could not parse get leaseIdParam %s" leaseIdParam)
                |> AsyncResult.ofResult
            let asOf = 
                match req.queryParam "asOf" with
                | Choice1Of2 asOfStr ->
                    asOfStr
                    |> DateTime.tryParse
                    |> Option.map AsOf
                | _ -> None
            let asAt = 
                match req.queryParam "asAt" with
                | Choice1Of2 asOfStr ->
                    asOfStr
                    |> DateTime.tryParse
                    |> Option.map AsAt
                | _ -> None
            let! result =
                match (asOf, asAt) with
                | (None, None) -> getLeaseStateResponse service leaseId Latest
                | (Some asOfDate, None) -> getLeaseStateResponse service leaseId asOfDate
                | (None, Some asAtDate) -> getLeaseStateResponse service leaseId asAtDate
                | (Some _, Some _) -> "only specify asOf or asAt, not both" |> AsyncResult.ofError
            return result
        }

let handleCreateLease
    (service:Service)
    : Handle =
    fun (ctx:HttpContext) ->
        let { request = { rawForm = body }} = ctx
        asyncResult {
            let! newLease =
                body
                |> LeaseSchema.deserializeFromBytes
                |> Result.map LeaseSchema.toDomain
                |> AsyncResult.ofResult
            let command = Create newLease
            do! service.execute newLease.LeaseId command
            return! getLeaseStateResponse service newLease.LeaseId Latest
        }

let handleTerminateLease
    (service:Service)
    : HandlePath<string> =
    fun leaseIdParam (ctx:HttpContext) ->
        let { request = req } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption "could not parse delete leaseIdParam"
                |> AsyncResult.ofResult
            let effDate = 
                match req.queryParam "effDate" with
                | Choice1Of2 effDateStr ->
                    effDateStr
                    |> DateTime.tryParse
                    |> Option.map UMX.tag<eventEffectiveDate>
                | _ -> None
                |> Option.defaultValue %DateTime.UtcNow
            let command = Terminate effDate
            do! service.execute leaseId command
            return! getLeaseStateResponse service leaseId Latest
        }

let handleSchedulePayment
    (service:Service)
    : HandlePath<string> =
    fun leaseIdParam (ctx:HttpContext) ->
        let { request = { rawForm = body } } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption (sprintf "could not parse schedule leaseIdParam %s" leaseIdParam)
                |> AsyncResult.ofResult
            let! payment =
                body
                |> PaymentSchema.deserializeFromBytes
                |> Result.map PaymentSchema.toScheduledPayment
                |> AsyncResult.ofResult
            let command = SchedulePayment payment
            do! service.execute leaseId command
            return! getLeaseStateResponse service leaseId Latest
        }

let handleReceivePayment
    (service:Service)
    : HandlePath<string> =
    fun leaseIdParam (ctx:HttpContext) ->
        let { request = { rawForm = body } } = ctx
        asyncResult {
            let! leaseId = 
                leaseIdParam 
                |> Guid.tryParse
                |> Option.map UMX.tag<leaseId>
                |> Result.ofOption (sprintf "could not parse receive payment leaseIdParam %s" leaseIdParam)
                |> AsyncResult.ofResult
            let! payment =
                body
                |> PaymentSchema.deserializeFromBytes
                |> Result.map PaymentSchema.toPayment
                |> AsyncResult.ofResult
            let command = ReceivePayment payment
            do! service.execute leaseId command
            return! getLeaseStateResponse service leaseId Latest
        }

let init (service:Service) =
    let handleGetLease' = handleGetLease service
    let handleCreateLease' = handleCreateLease service
    let handleSchedulePayment' = handleSchedulePayment service
    let handleReceivePayment' = handleReceivePayment service
    let handleTerminateLease' = handleTerminateLease service
    choose
        [ path "/lease" >=> choose 
            [ POST >=> (createHandler handleCreateLease') >=> JSON ]
          pathRegex "/lease/[^/]+?$" >=> choose
            [ GET >=> pathScan "/lease/%s" (createPathHandler handleGetLease') >=> JSON
              DELETE >=> pathScan "/lease/%s" (createPathHandler handleTerminateLease') >=> JSON ]
          pathRegex "/lease/[^/]+?/[^/]+?$" >=> choose
            [ POST >=> choose
                  [ pathScan "/lease/%s/schedule" (createPathHandler handleSchedulePayment') >=> JSON
                    pathScan "/lease/%s/payment" (createPathHandler handleReceivePayment') >=> JSON ] ]
          NOT_FOUND "handler not implemented" ]
