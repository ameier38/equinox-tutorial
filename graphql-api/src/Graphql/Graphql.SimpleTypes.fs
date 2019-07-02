namespace Graphql

open Google.Protobuf.WellKnownTypes
open Google.Type
open System

module DateTime =
    let parse (s:string) = DateTime.Parse(s)
    let toUtc (dt:DateTime) = DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    let toProtoTimestamp (dt:DateTime) = dt |> toUtc |> Timestamp.FromDateTime
    let toProtoDate (dt:DateTime) = dt |> toUtc |> Date.FromDateTime

module Money =
    let fromDecimal (value:decimal) =
        Money(DecimalValue = value, CurrencyCode = "USD")
