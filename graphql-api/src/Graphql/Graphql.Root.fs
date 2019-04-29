module Graphql.Root

open FSharp.Data.GraphQL.Types
open Lease

type Root = { _empty: bool option }

let leaseField
    (leaseService:LeaseService) =
    Define.Field(
        name = "lease",
        typedef = Nullable LeaseObservationType,
        description = "observe a lease at a point in time",
        args = [ Define.Input("leaseId", ID); Define.Input("asOfDate", AsOfDateInputType)],
        resolve = (fun ctx _ ->
            result {
                let! leaseId = ctx.TryArg("leaseId") |> Result.ofOption "leaseId is not defined"
                let asOfDateInputOpt = ctx.TryArg<AsOfDateInput>("asOfDate")
                return! leaseService.GetLease(leaseId, asOfDateInputOpt)
            } |> Result.bimap Some (failwithf "Error: %s")))

let Query
    (leaseService:LeaseService) =
    Define.Object<Root>(
        name = "Query",
        fields = [ leaseField leaseService ])
