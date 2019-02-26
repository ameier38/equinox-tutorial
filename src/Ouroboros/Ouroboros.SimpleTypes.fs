namespace Ouroboros

open FSharp.UMX
open System
open SimpleType

module Guid =
    let inline toStringN (x: Guid) = x.ToString "N"

type Entity = private Entity of String50
module Entity =
    let value (Entity entity) = entity |> String50.value
    let create entity = entity |> String50.create |> Result.map Entity

type [<Measure>] eventId
type EventId = int<eventId>

type [<Measure>] createdDate
type CreatedDate = DateTime<createdDate>

type [<Measure>] effectiveDate
type EffectiveDate = DateTime<effectiveDate>
