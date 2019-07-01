module Graphql.Root

open FSharp.Data.GraphQL.Types
open Graphql.Lease

type Root = { _empty: bool option }

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
            createLeaseField leaseClient
            schedulePaymentField leaseClient
            receivePaymentField leaseClient
            terminateLeaseField leaseClient
        ]
    )
