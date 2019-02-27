namespace Lease

open FSharp.UMX
open Ouroboros
open SimpleType
open System
open System.Text

[<Measure>] type leaseId
type LeaseId = Guid<leaseId>
module LeaseId = let toStringN (value: LeaseId) = Guid.toStringN %value

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
    let tryParse (s:string) =
        match Guid.TryParse(s) with
        | (true, d) -> Some d
        | _ -> None
