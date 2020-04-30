namespace Vehicle

open FSharp.UMX
open Grpc.Core
open System

type [<Measure>] vehicleId
type VehicleId = Guid<vehicleId>

module RpcException =
    let raiseInternal (msg:string) =
        RpcException(Status(StatusCode.Internal, msg)) |> raise
    let raiseAlreadyExists (msg:string) =
        RpcException(Status(StatusCode.AlreadyExists, msg)) |> raise
    let raiseNotFound (msg:string) =
        RpcException(Status(StatusCode.NotFound, msg)) |> raise

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
