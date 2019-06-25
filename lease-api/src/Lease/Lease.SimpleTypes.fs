namespace Lease

open FSharp.UMX
open Grpc.Core
open Microsoft.FSharp.Reflection
open System
open System.Text

[<Measure>] type month

[<Measure>] type usd
type USD = decimal<usd>

type MonthlyPaymentAmount = decimal<usd/month>

[<Measure>] type userId
type UserId = Guid<userId>

[<Measure>] type leaseId
type LeaseId = Guid<leaseId>

[<Measure>] type paymentId
type PaymentId = Guid<paymentId>

[<Measure>] type paymentDate
type PaymentDate = DateTime<paymentDate>

[<Measure>] type eventId
type EventId = int<eventId>

[<Measure>] type eventType
type EventType = string<eventType>

[<Measure>] type eventEffectiveDate
type EventEffectiveDate = DateTime<eventEffectiveDate>

[<Measure>] type eventEffectiveOrder
type EventEffectiveOrder = int<eventEffectiveOrder>

[<Measure>] type eventCreatedTime
type EventCreatedTime = DateTime<eventCreatedTime>

[<Measure>] type leaseStartDate
type LeaseStartDate = DateTime<leaseStartDate>

[<Measure>] type leaseMaturityDate
type LeaseMaturityDate = DateTime<leaseMaturityDate>

[<Measure>] type pageToken
type PageToken = string<pageToken>

[<Measure>] type pageSize
type PageSize = int<pageSize>

module DateTime =
    let toUtc (dt:DateTime) = DateTime.SpecifyKind(dt, DateTimeKind.Utc)

module Decimal =
    let round (decimalPlaces:int) (value:decimal<'u>) = 
        Decimal.Round(%value, decimalPlaces) |> UMX.tag<'u>

module String =
    let toBytes (s:string) = s |> Encoding.UTF8.GetBytes
    let fromBytes (bytes:byte []) = bytes |> Encoding.UTF8.GetString
    let lower (s:string) = s.ToLower()
    let toBase64 (s:string) = s |> toBytes |> Convert.ToBase64String
    let fromBase64 (s:string) = s |> Convert.FromBase64String |> fromBytes

module Guid =
    let inline toStringN (x: Guid) = x.ToString "N"
    let tryParse (s:string) =
        match Guid.TryParse(s) with
        | (true, d) -> Some d
        | _ -> None

module Int =
    let tryParse (s:string) =
        match Int32.TryParse(s) with
        | (true, i) -> Some i
        | _ -> None

module Union =
    let toString (x:'a) = 
        match FSharpValue.GetUnionFields(x, typeof<'a>) with
        | case, _ -> case.Name

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

module UserId = 
    let toStringN (value: UserId) = Guid.toStringN %value
    let parse (x:string) : UserId = 
        match Guid.tryParse x with
        | Some userId -> %userId
        | None -> 
            let msg = sprintf "could not parse %s into UserId" x
            RpcException(Status(StatusCode.InvalidArgument, msg))
            |> raise

module LeaseId = 
    let toStringN (value:LeaseId) = Guid.toStringN %value
    let parse (x:string) : LeaseId = 
        match Guid.tryParse x with
        | Some leaseId -> %leaseId
        | None -> 
            let msg = sprintf "could not parse %s into LeaseId" x
            RpcException(Status(StatusCode.InvalidArgument, msg))
            |> raise

module PaymentId = 
    let toStringN (value: PaymentId) = Guid.toStringN %value
    let parse (x:string) : PaymentId = 
        match Guid.tryParse x with
        | Some paymentId -> %paymentId
        | None -> 
            let msg = sprintf "could not parse %s into PaymentId" x
            RpcException(Status(StatusCode.InvalidArgument, msg))
            |> raise
