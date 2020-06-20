namespace Shared

open System
open System.IO
open System.Text
open Google.Protobuf.WellKnownTypes
open Google.Type

module String =
    let fromBytes (bytes:byte[]) =
        Encoding.UTF8.GetString(bytes)
    let isNullOrWhiteSpace (str : string) =
        String.IsNullOrWhiteSpace(str)

module DateTime =
    let parse (s:string) = DateTime.Parse(s)
    let toUtc (dt:DateTime) = DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    let toProtoTimestamp (dt:DateTime) = dt |> toUtc |> Timestamp.FromDateTime
    let toProtoDate (dt:DateTime) = dt |> toUtc |> Date.FromDateTime

module Money =
    let fromDecimal (value:decimal) =
        Money(DecimalValue = value, CurrencyCode = "USD")

module Env = 
    let getEnv (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrEmpty(value) -> defaultValue
        | value -> value
    let getSecret (secretsDir:string) (secretName:string) (secretKey:string) (defaultEnv:string) (defaultValue:string) =
        let secretPath = Path.Combine(secretsDir, secretName, secretKey)
        if File.Exists(secretPath) then
            File.ReadAllText(secretPath).Trim()
        else
            getEnv defaultEnv defaultValue

module Operators =
    let (!!) (value:float) = value |> decimal |> Money.fromDecimal
    let (!@) (dt:DateTime) = dt |> DateTime.toProtoDate
    let (!@@) (dt:DateTime) = dt |> DateTime.toProtoTimestamp
