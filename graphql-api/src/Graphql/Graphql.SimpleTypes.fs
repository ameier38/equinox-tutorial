namespace Graphql

open Google.Protobuf.WellKnownTypes
open Google.Type
open System

module DateTime =
    let toUtc (dt:DateTime) = DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    let toProtoTimestamp (dt:DateTime) = dt |> toUtc |> Timestamp.FromDateTime
    let toProtoDate (dt:DateTime) = dt |> toUtc |> Date.FromDateTime
