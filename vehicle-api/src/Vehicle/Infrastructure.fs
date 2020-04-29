namespace Vehicle

open FSharp.UMX
open Grpc.Core
open System

type [<Measure>] vehicleId
type VehicleId = Guid<vehicleId>

module Guid =
    let inline toStringN (x: Guid<'t>) =
        let x = x |> UMX.untag
        x.ToString("N")
    let tryParse<[<Measure>] 't>(s:string): Guid<'t> option =
        match Guid.TryParse(s) with
        | (true, d) -> d |> UMX.tag<'t> |> Some
        | _ -> None

module RpcException =
    let raiseInternal (msg:string) =
        RpcException(Status(StatusCode.Internal, msg)) |> raise
    let raiseAlreadyExists (msg:string) =
        RpcException(Status(StatusCode.AlreadyExists, msg)) |> raise
    let raiseNotFound (msg:string) =
        RpcException(Status(StatusCode.NotFound, msg)) |> raise

module Env = 
    let getEnv (key:string) (defaultValueOpt:string option) =
        match Environment.GetEnvironmentVariable(key), defaultValueOpt with
        | value, Some defaultValue when String.IsNullOrEmpty(value) -> defaultValue
        | value, None when String.IsNullOrEmpty(value) -> failwithf "envVar %s is not defined" key
        | value, _ -> value
