module Graphql.Root

open FSharp.Data.GraphQL.Types
open Graphql.Lease.Client
open Graphql.Lease.Fields

type Root = { _empty: bool option }

let Query
    (leaseClient:LeaseClient) =
    Define.Object<Root>(
        name = "Query",
        fields = [ 
            getLeaseField leaseClient 
            listLeasesField leaseClient
        ]
    )

let Mutation
    (leaseClient:LeaseClient) =
    Define.Object<Root>(
        name = "Mutation",
        fields = [
            createLeaseField leaseClient
            schedulePaymentField leaseClient
            receivePaymentField leaseClient
            terminateLeaseField leaseClient
            deleteLeaseEventField leaseClient
        ]
    )
