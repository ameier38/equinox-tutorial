namespace Lease

open FSharp.UMX
open Ouroboros
open SimpleType
open System

[<Measure>] type leaseId
type LeaseId = Guid<leaseId>
module LeaseId = let toStringN (value: LeaseId) = Guid.toStringN %value
