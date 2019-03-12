module Lease.Service

open FSharp.UMX
open Lease.Dto
open Lease.Aggregate
open Lease.Store
open Equinox.EventStore
open Serilog.Core
open Suave
open System

type Service(aggregate:Aggregate, store:Store, logger:Logger) =
    let (|AggregateId|) (leaseId: LeaseId) = Equinox.AggregateId(aggregate.Entity, LeaseId.toStringN leaseId)
    let (|Stream|) (AggregateId leaseId) = Equinox.Stream(logger, store.Resolve leaseId, 3)
    let execute (Stream stream) command = stream.Transact(aggregate.Interpret command)
    let query (Stream stream) (obsDate:ObservationDate) =
        stream.Query(fun leaseEvents -> 
            leaseEvents 
            |> aggregate.Reconstitute obsDate)

    let getLeaseStateResponse =
        fun (leaseId:LeaseId) (obsDate:ObservationDate) ->
            async {
                let! state = query leaseId obsDate
                return!
                    state
                    |> LeaseStateSchema.fromDomain
                    |> Result.map LeaseStateSchema.serializeToJson
                    |> AsyncResult.ofResult
            }

    member __.Get
        : string -> HttpContext -> AsyncResult<string,string> =
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
                    | (None, None) -> getLeaseStateResponse leaseId Latest
                    | (Some asOfDate, None) -> getLeaseStateResponse leaseId asOfDate
                    | (None, Some asAtDate) -> getLeaseStateResponse leaseId asAtDate
                    | (Some _, Some _) -> "only specify asOf or asAt, not both" |> AsyncResult.ofError
                return result
            }

    member __.Create
        : HttpContext -> AsyncResult<string,string> =
        fun (ctx:HttpContext) ->
            let { request = { rawForm = body }} = ctx
            asyncResult {
                let! newLease =
                    body
                    |> LeaseSchema.deserializeFromBytes
                    |> Result.map LeaseSchema.toDomain
                    |> AsyncResult.ofResult
                let command = Create newLease
                do! execute newLease.LeaseId command
                return! getLeaseStateResponse newLease.LeaseId Latest
            }

    member __.Terminate
        : string -> HttpContext -> AsyncResult<string,string> =
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
                do! execute leaseId command
                return! getLeaseStateResponse leaseId Latest
            }

    member __.SchedulePayment
        : string -> HttpContext -> AsyncResult<string,string> =
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
                do! execute leaseId command
                return! getLeaseStateResponse leaseId Latest
            }

    member __.ReceivePayment
        : string -> HttpContext -> AsyncResult<string,string> =
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
                do! execute leaseId command
                return! getLeaseStateResponse leaseId Latest
            }

