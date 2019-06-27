module Graphql.Root

open FSharp.Data.GraphQL.Types
open Lease

type Root = { _empty: bool option }

let leaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "lease",
        typedef = Nullable LeaseObservationType,
        description = "get a lease at a point in time",
        args = [ Define.Input("leaseId", ID); Define.Input("asOfDate", AsOfDateInputType) ],
        resolve = (fun ctx _ ->
            let leaseId = ctx.Arg("leaseId")
            let asOfDate = 
                ctx.TryArg<AsOfDateInput>("asOfDate")
                |> Option.map AsOfDate.fromInput
                |> Option.defaultValue (AsOfDate.getDefault())
            leaseClient.GetLease(leaseId, asOfDate) |> Some
        ))

let Query
    (leaseClient:LeaseClient) =
    Define.Object<Root>(
        name = "Query",
        fields = [ leaseField leaseClient ])
