namespace Vehicle

open FSharp.UMX
open Grpc.Core
open System
open System.Text

type [<Measure>] userId
type UserId = string<userId>

type [<Measure>] vehicleId
type VehicleId = Guid<vehicleId>

type [<Measure>] pageToken
type PageToken = string<pageToken>

type [<Measure>] pageSize
type PageSize = int<pageSize>

module RpcException =
    let raiseInternal (msg:string) =
        RpcException(Status(StatusCode.Internal, msg)) |> raise
    let raiseAlreadyExists (msg:string) =
        RpcException(Status(StatusCode.AlreadyExists, msg)) |> raise
    let raiseNotFound (msg:string) =
        RpcException(Status(StatusCode.NotFound, msg)) |> raise
    let raisePermissionDenied (msg:string) =
        RpcException(Status(StatusCode.PermissionDenied, msg)) |> raise

module Guid =
    let inline toStringN (x: Guid<'t>) =
        let x = x |> UMX.untag
        x.ToString("N")
    let parse<[<Measure>] 't>(s:string): Guid<'t> =
        match Guid.TryParse(s) with
        | (true, d) ->
            UMX.tag<'t> d
        | _ -> 
            sprintf "could not parse %s as Guid" s
            |> RpcException.raiseInternal

module String =
    let toBytes (s:string) = s |> Encoding.UTF8.GetBytes
    let fromBytes (bytes:byte []) = bytes |> Encoding.UTF8.GetString
    let lower (s:string) = s.ToLower()
    let replace (oldValue:string) (newValue:string) (s:string) = s.Replace(oldValue, newValue)
    let toBase64 (s:string) = s |> toBytes |> Convert.ToBase64String
    let fromBase64 (s:string) = s |> Convert.FromBase64String |> fromBytes

module VehicleId =
    let toString (vehicleId:VehicleId) = Guid.toStringN vehicleId
    let fromString (s:string) = Guid.parse<vehicleId> s
    let create () = Guid.NewGuid() |> UMX.tag<vehicleId>

module UserId =
    let toString (userId:UserId) = UMX.untag userId
    let fromString (s:string) = UMX.tag<userId> s

module PageToken =
    let prefix = "index-"
    let toString (pageToken:PageToken) = UMX.untag pageToken
    let fromString (s:string) = UMX.tag<pageToken> s
    let decode (t:PageToken) : int64 =
        match %t with
        | "" -> 0L
        | token -> 
            token 
            |> String.fromBase64 
            |> String.replace prefix ""
            |> int64
    let encode (cursor:int64) : PageToken =
        sprintf "%s%d" prefix cursor 
        |> String.toBase64
        |> UMX.tag<pageToken>

module PageSize =
    let fromInt (i:int) = UMX.tag<pageSize> i
    let toInt (pageSize:PageSize) =
        pageSize
        |> UMX.untag
