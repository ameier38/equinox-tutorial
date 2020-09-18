namespace Server

open Grpc.Core

module RpcException =
    let raiseInternal (msg:string) =
        RpcException(Status(StatusCode.Internal, msg)) |> raise
    let raiseAlreadyExists (msg:string) =
        RpcException(Status(StatusCode.AlreadyExists, msg)) |> raise
    let raiseNotFound (msg:string) =
        RpcException(Status(StatusCode.NotFound, msg)) |> raise
    let raisePermissionDenied (msg:string) =
        RpcException(Status(StatusCode.PermissionDenied, msg)) |> raise

module Result =
    type ResultBuilder() =
        member _.Bind(x, f) =
            match x with
            | Error error -> Error error
            | Ok v -> f v
        member _.Return(x) =
            Ok x

    module Builders =
        let result = ResultBuilder()
    