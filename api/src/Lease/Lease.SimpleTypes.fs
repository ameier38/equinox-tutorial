namespace Lease

open FSharp.UMX
open SimpleType
open System
open System.Text

module DateTime =
    let tryParse (s:string) =
        match DateTime.TryParse(s) with
        | (true, d) -> d.ToUniversalTime() |> Some
        | _ -> None
    let replaceDay (day:int) (d:DateTime) = DateTime(d.Year, d.Month, day)
    let addMonths (months:int) (d:DateTime) = d.AddMonths(months)
    let addDays (days:int) (d:DateTime) = d.AddDays(days |> float)
    let toMonthEnd (d:DateTime) = d |> replaceDay 1 |> addMonths 1 |> addDays -1
    let isMonthEnd (d:DateTime) = d = (d |> toMonthEnd)
    let monthRange (startDate:DateTime) (endDate:DateTime) =
        let addDaysToStartDate i = startDate |> addDays i
        let atOrBeforeEndDate d = d <= endDate
        let isMonthEnd d = d |> isMonthEnd
        Seq.initInfinite id
        |> Seq.map addDaysToStartDate
        |> Seq.takeWhile atOrBeforeEndDate
        |> Seq.filter isMonthEnd

module String =
    let toBytes (s:string) = s |> Encoding.UTF8.GetBytes
    let fromBytes (bytes:byte []) = bytes |> Encoding.UTF8.GetString
    let lower (s:string) = s.ToLower()

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

type Entity = private Entity of String50
module Entity =
    let value (Entity entity) = entity |> String50.value
    let create entity = entity |> String50.create |> Result.map Entity

[<Measure>] type month

[<Measure>] type usd
type USD = decimal<usd>

type MonthlyPaymentAmount = decimal<usd/month>

[<Measure>] type userId
type UserId = Guid<userId>
module UserId = let toStringN (value: UserId) = Guid.toStringN %value

[<Measure>] type leaseId
type LeaseId = Guid<leaseId>
module LeaseId = let toStringN (value: LeaseId) = Guid.toStringN %value

[<Measure>] type paymentId
type PaymentId = Guid<paymentId>
module PaymentId = let toStringN (value: PaymentId) = Guid.toStringN %value

[<Measure>] type eventId
type EventId = int<eventId>

[<Measure>] type eventEffectiveDate
type EventEffectiveDate = DateTime<eventEffectiveDate>

[<Measure>] type eventEffectiveOrder
type EventEffectiveOrder = int<eventEffectiveOrder>

[<Measure>] type eventCreatedDate
type EventCreatedDate = DateTime<eventCreatedDate>

[<Measure>] type asOnDate
type AsOnDate = DateTime<asOnDate>

[<Measure>] type asAtDate
type AsAtDate = DateTime<asAtDate>

[<Measure>] type leaseStartDate
type LeaseStartDate = DateTime<leaseStartDate>

[<Measure>] type leaseMaturityDate
type LeaseMaturityDate = DateTime<leaseMaturityDate>
