namespace Shared

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

module Env = 
    let getEnv (key:string) (defaultValueOpt:string option) =
        match Environment.GetEnvironmentVariable(key), defaultValueOpt with
        | value, Some defaultValue when String.IsNullOrEmpty(value) -> defaultValue
        | value, None when String.IsNullOrEmpty(value) -> failwithf "envVar %s is not defined" key
        | value, _ -> value
