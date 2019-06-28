module Graphql.Root

open FSharp.Data.GraphQL.Types
open Lease

type Root = { _empty: bool option }

let leaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "getLease",
        typedef = Nullable LeaseObservationType,
        description = "get a lease at a point in time",
        args = [ 
            Define.Input("leaseId", ID)
            Define.Input("asOfDate", AsOfDateInputType) 
        ],
        resolve = (fun ctx _ ->
            let leaseId = ctx.Arg("leaseId")
            let asOfDate = 
                ctx.TryArg<AsOfDateInput>("asOfDate")
                |> Option.map AsOfDate.fromInput
                |> Option.defaultValue (AsOfDate.getDefault())
            leaseClient.GetLease(leaseId, asOfDate) |> Some
        )
    )

let leasesField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "listLeases",
        typedef = ListOf LeaseType,
        description = "list existing leases",
        args = [ 
            Define.Input("asOfDate", AsOfDateInputType) 
            Define.Input("pageSize", Int)
            Define.Input("pageToken", String)
        ],
        resolve = (fun ctx _ ->
            let asOfDate = 
                ctx.TryArg<AsOfDateInput>("asOfDate")
                |> Option.map AsOfDate.fromInput
                |> Option.defaultValue (AsOfDate.getDefault())
            let pageSize = ctx.TryArg("pageSize") |> Option.defaultValue 20
            let pageToken = ctx.TryArg("pageToken") |> Option.defaultValue ""
            leaseClient.ListLeases(asOfDate, pageSize, pageToken)
        )
    )

let Query
    (leaseClient:LeaseClient) =
    Define.Object<Root>(
        name = "Query",
        fields = [ 
            leaseField leaseClient 
            leasesField leaseClient
        ]
    )

let Mutation
    (leaseClient:LeaseClient) =
    Define.Object<Root>(
        name = "Mutation",
        fields = [
            Define.Field(
                name = "createLease",
                typedef = CreatedLeaseType,
                description = "create a new lease",
                args = [
                    Define.Input("userId", ID)
                    Define.Input("startDate", Date)
                    Define.Input("maturityDate", Date)
                    Define.Input("monthlyPaymentAmount", Float)],
                resolve = (fun ctx _ ->
                    let userId = ctx.Arg("userId")
                    let startDate = ctx.Arg("startDate")
                    let maturityDate = ctx.Arg("maturityDate")
                    let monthlyPaymentAmount = ctx.Arg("monthlyPaymentAmount")
                    leaseClient.CreateLease(userId, startDate, maturityDate, monthlyPaymentAmount))
            )
        ]
    )
