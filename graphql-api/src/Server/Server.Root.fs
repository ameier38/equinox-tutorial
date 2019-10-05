module Server.Root

open FSharp.Data.GraphQL.Types
open Lease.Client
open Lease.Fields

type Root = { _empty: bool option }

let Query
    (leaseClient:LeaseClient) =
    Define.Object<Root>(
        name = "Query",
        fields = [ 
            getLeaseField leaseClient 
            listLeasesField leaseClient
            listLeaseEventsField leaseClient
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
