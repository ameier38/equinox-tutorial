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

[<Measure>] type leaseId
type LeaseId = Guid<leaseId>
module LeaseId = let toStringN (value: LeaseId) = Guid.toStringN %value

type [<Measure>] eventId
type EventId = int<eventId>

type [<Measure>] eventType
type EventType = string<eventType>

type [<Measure>] eventCreatedDate
type EventCreatedDate = DateTime<eventCreatedDate>

type [<Measure>] eventEffectiveDate
type EventEffectiveDate = DateTime<eventEffectiveDate>

type [<Measure>] scheduledPaymentAmount
type ScheduledPaymentAmount = decimal<scheduledPaymentAmount>

type [<Measure>] paymentAmount
type PaymentAmount = decimal<paymentAmount>

type [<Measure>] monthlyPaymentAmount
type MonthlyPaymentAmount = decimal<monthlyPaymentAmount>
