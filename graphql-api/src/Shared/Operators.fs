namespace Shared

open System

module Operators =
    let (!!) (value:float) = value |> decimal |> Money.fromDecimal
    let (!@) (dt:DateTime) = dt |> DateTime.toProtoDate
    let (!@@) (dt:DateTime) = dt |> DateTime.toProtoTimestamp
