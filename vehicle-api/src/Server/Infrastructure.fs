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
