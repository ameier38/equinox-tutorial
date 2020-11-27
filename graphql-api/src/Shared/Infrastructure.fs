namespace Shared

open System
open System.IO
open System.Text
open System.Text.RegularExpressions
open Google.Protobuf.WellKnownTypes
open Google.Type

module Env = 
    let getEnv (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrEmpty(value) -> defaultValue
        | value -> value

type Secret<'T> = private Secret of 'T 
    with member this.Value() = let (Secret value) = this in value
module Secret =
    let create (x:'T) = Secret x
    let getSecret (secretsDir:string) (secretName:string) (secretKey:string) (defaultEnv:string) (defaultValue:string) =
        let secretPath = Path.Combine(secretsDir, secretName, secretKey)
        if File.Exists(secretPath) then
            File.ReadAllText(secretPath).Trim()
            |> create
        else
            Env.getEnv defaultEnv defaultValue
            |> create

module String =
    let fromBytes (bytes:byte[]) =
        Encoding.UTF8.GetString(bytes)
    let isNullOrWhiteSpace (str : string) =
        String.IsNullOrWhiteSpace(str)

module DateTime =
    let parse (s:string) = DateTime.Parse(s)
    let toUtc (dt:System.DateTime) = DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    let toProtoTimestamp (dt:System.DateTime) = dt |> toUtc |> Timestamp.FromDateTime
    let toProtoDate (dt:System.DateTime) = dt |> toUtc |> Date.FromDateTime

    module Operators =
        let (!@) (dt:System.DateTime) = dt |> toProtoDate
        let (!@@) (dt:System.DateTime) = dt |> toProtoTimestamp

module Money =
    let fromDecimal (value:decimal) =
        Money(DecimalValue = value, CurrencyCode = "USD")

    module Operators =
        let (!!) (value:float) = value |> decimal |> fromDecimal

module Regex =
    let (|Match|_|) (pattern:string) (s:string) =
        let m = Regex.Match(s, pattern)
        if m.Success then Some(List.tail [for g in m.Groups -> g.Value])
        else None
