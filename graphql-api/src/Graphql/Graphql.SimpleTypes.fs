namespace Graphql

open Google.Protobuf.WellKnownTypes
open System

module DateTime =
    let toUtc (dt:DateTime) = DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    let toTimestamp (dt:DateTime) = dt |> toUtc |> Timestamp.FromDateTime