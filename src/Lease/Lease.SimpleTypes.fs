namespace Lease

open FSharp.UMX
open SimpleType
open System
open System.Text

module DateTime =
    let tryParse (s:string) =
        match DateTime.TryParse(s) with
        | (true, d) -> Some d
        | _ -> None

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

type [<Measure>] createdDate
type CreatedDate = DateTime<createdDate>

type [<Measure>] effectiveDate
type EffectiveDate = DateTime<effectiveDate>
