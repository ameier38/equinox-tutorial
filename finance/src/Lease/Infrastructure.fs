namespace Lease

open FSharp.UMX
open Grpc.Core
open System
open System.Text

[<Measure>] type day
type Day = int<day>

[<Measure>] type month

[<Measure>] type year

[<Measure>] type mile
type Mile = int<mile>

[<Measure>] type usd
type USD = decimal<usd>

[<Measure>] type userId
type UserId = Guid<userId>

[<Measure>] type vehicleId
type VehicleId = Guid<vehicleId>

[<Measure>] type leaseId
type LeaseId = Guid<leaseId>

[<Measure>] type transactionId
type TransactionId = Guid<transactionId>

[<Measure>] type eventCreatedAt
type EventCreatedAt = DateTimeOffset<eventCreatedAt>

[<Measure>] type eventEffectiveAt
type EventEffectiveAt = DateTimeOffset<eventEffectiveAt>

[<Measure>] type eventEffectiveOrder
type EventEffectiveOrder = int<eventEffectiveOrder>

[<Measure>] type eventType
type EventType = string<eventType>

[<Measure>] type pageToken
type PageToken = string<pageToken>

[<Measure>] type pageSize
type PageSize = int<pageSize>

module DateTimeOffset =
    let addDays (n:float) (dt:DateTimeOffset<'t>) =
        let dt = dt |> UMX.untag
        dt.AddDays(n)
        |> UMX.tag<'t>
    let addTicks (n:int64) (dt:DateTimeOffset<'t>) =
        let dt = dt |> UMX.untag
        dt.AddTicks(n)
        |> UMX.tag<'t>
    let toStartOfDay (dt:DateTimeOffset<'t>) =
        let dt = dt |> UMX.untag
        dt.Date
        |> DateTimeOffset
        |> UMX.tag<'t>
    let toEndOfDay (dt:DateTimeOffset<'t>) =
        dt
        |> toStartOfDay
        |> addDays 1.0
        |> addTicks -1L
    let range (periodStart:DateTimeOffset<'t>) (periodEnd:DateTimeOffset<'t>) =
        let periodStart = periodStart |> toStartOfDay
        let periodEnd = periodEnd |> toStartOfDay
        Seq.initInfinite float
        |> Seq.map (fun idx -> periodStart |> addDays idx)
        |> Seq.takeWhile (fun dt -> dt < periodEnd)

module Decimal =
    let round (decimalPlaces:int) (value:decimal<'u>) = 
        Decimal.Round(%value, decimalPlaces) |> UMX.tag<'u>

module String =
    let toBytes (s:string) = s |> Encoding.UTF8.GetBytes
    let fromBytes (bytes:byte []) = bytes |> Encoding.UTF8.GetString
    let lower (s:string) = s.ToLower()
    let replace (oldValue:string) (newValue:string) (s:string) = s.Replace(oldValue, newValue)
    let toBase64 (s:string) = s |> toBytes |> Convert.ToBase64String
    let fromBase64 (s:string) = s |> Convert.FromBase64String |> fromBytes

module Guid =
    let inline toStringN (x: Guid<'t>) =
        let x = x |> UMX.untag
        x.ToString("N")
    let tryParse<[<Measure>] 't>(s:string): Guid<'t> option =
        match Guid.TryParse(s) with
        | (true, d) -> d |> UMX.tag<'t> |> Some
        | _ -> None

module Int =
    let tryParse (s:string) =
        match Int32.TryParse(s) with
        | (true, i) -> Some i
        | _ -> None

module Money =
    let toUSD (money:Google.Type.Money) =
        match money.CurrencyCode with
        | "USD" -> money.DecimalValue |> UMX.tag<usd>
        | currCode -> 
            let msg = sprintf "%s is not a supported currency code" currCode
            RpcException(Status(StatusCode.InvalidArgument, msg))
            |> raise
    let create units nanos =
        Google.Type.Money(Units = units, Nanos = nanos, CurrencyCode = "USD")
    let fromUSD (d:USD) =
        let value = d |> UMX.untag |> Decimal.round 9 
        Google.Type.Money(DecimalValue = value, CurrencyCode = "USD")

module RpcException =
    let raiseInternal (msg:string) =
        RpcException(Status(StatusCode.Internal, msg)) |> raise
    let raiseAlreadyExists (msg:string) =
        RpcException(Status(StatusCode.AlreadyExists, msg)) |> raise

module PageToken =
    let prefix = "index-"
    let decode (t:PageToken) : int =
        match %t with
        | "" -> 0
        | token -> 
            token 
            |> String.fromBase64 
            |> String.replace prefix ""
            |> int 
    let encode (cursor:int) : PageToken =
        sprintf "%s%d" prefix cursor 
        |> String.toBase64
        |> UMX.tag<pageToken>

// ref: https://cloud.google.com/apis/design/design_patterns#list_pagination
// // pageToken contains the zero-based index of the starting element
module Pagination =
    type PageInfo<'T> =
        { PrevPageToken: PageToken
          NextPageToken: PageToken
          Page: seq<'T> }
    let getPage (pageToken:PageToken) (pageSize:PageSize) (s:seq<'T>) =
        let start = pageToken |> PageToken.decode
        let cnt = s |> Seq.length
        let remaining = cnt - start
        let toTake = min remaining %pageSize
        let page = s |> Seq.skip start |> Seq.take toTake
        let prevPageToken =
            match start - %pageSize with
            | c when c <= 0 -> "" |> UMX.tag<pageToken>
            | c -> c |> PageToken.encode
        let nextPageToken = 
            match start + %pageSize with
            | c when c >= cnt -> "" |> UMX.tag<pageToken>
            | c -> c |> PageToken.encode
        { PrevPageToken = prevPageToken
          NextPageToken = nextPageToken
          Page = page }

module Env = 
    let getEnv (key:string) (defaultValueOpt:string option) =
        match Environment.GetEnvironmentVariable(key), defaultValueOpt with
        | value, Some defaultValue when String.IsNullOrEmpty(value) -> defaultValue
        | value, None when String.IsNullOrEmpty(value) -> failwithf "envVar %s is not defined" key
        | value, _ -> value

module Operators =
    let (!!) (value:decimal<'u>) = %value |> Money.fromUSD
    let (!@) (value:DateTimeOffset<'u>) = %value |> Google.Type.Date.FromDateTimeOffset
    let (!@@) (value:DateTimeOffset<'u>) = %value |> Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset
