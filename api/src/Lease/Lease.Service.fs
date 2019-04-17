module Lease.Service

open FSharp.UMX
open Lease.Store
open Serilog.Core
open System

type Service(store:Store, logger:Logger) =
    let (|AggregateId|) (leaseId: LeaseId) = Equinox.AggregateId("lease", LeaseId.toStringN leaseId)
    let (|Stream|) (AggregateId leaseId) = Equinox.Stream(logger, store.Resolve leaseId, 3)
    let execute (Stream stream) command = stream.Transact(Aggregate.decide command)
    let query (Stream stream) (asOfDate:AsOfDate) = stream.Query(Aggregate.reconstitute asOfDate None)
