module Graphql.Lease

open FSharp.Data.GraphQL.Types
open Grpc.Core

type AsOfDateInput =
    { AsAt: string option
      AsOn: string option }
let AsOfDateInputType =
    Define.InputObject<AsOfDateInput>(
        name = "AsOfDate",
        description = "As of date",
        fields = [
            Define.Input(
                name = "asAt",
                typedef = Nullable String,
                description = "Filter events created at or before this date")
            Define.Input(
                name = "asOn",
                typedef = Nullable String,
                description = "Filter events effective on or before this date") 
        ])

type AsOfDate =
    { AsAt: System.DateTime
      AsOn: System.DateTime }
module AsOfDate =
    let getDefault () =
        let now = System.DateTime.UtcNow
        { AsAt = now
          AsOn = now }
    let fromInput (input:AsOfDateInput) =
        let now = System.DateTime.UtcNow
        { AsOfDate.AsAt =
            input.AsAt
            |> Option.map System.DateTime.Parse
            |> Option.defaultValue now
          AsOfDate.AsOn =
            input.AsOn
            |> Option.map System.DateTime.Parse
            |> Option.defaultValue now }
    let toProto (asOfDate:AsOfDate) =
        Tutorial.Lease.V1.AsOfDate(
            AsAtTime = (asOfDate.AsAt |> DateTime.toProtoTimestamp),
            AsOnDate = (asOfDate.AsOn |> DateTime.toProtoDate))

type LeaseStatus =
    | Outstanding
    | Terminated
module LeaseStatus =
    let fromProto (proto:Tutorial.Lease.V1.LeaseStatus) =
        match proto with
        | Tutorial.Lease.V1.LeaseStatus.Outstanding -> Outstanding
        | Tutorial.Lease.V1.LeaseStatus.Terminated -> Terminated
        | other -> failwithf "invalid LeaseStatus %A" other
let LeaseStatusType =
    Define.Enum(
        name = "LeaseStatus",
        options = [
            Define.EnumValue("Outstanding", Outstanding)
            Define.EnumValue("Terminated", Terminated) ],
        description = "Status of the lease")

type Lease =
    { 
        LeaseId: string 
        UserId: string
        StartDate: System.DateTime 
        MaturityDate: System.DateTime 
        MonthlyPaymentAmount: decimal
    }
module Lease =
    let fromProto (proto:Tutorial.Lease.V1.Lease) =
        { 
            LeaseId = proto.LeaseId 
            UserId = proto.UserId 
            StartDate = proto.StartDate.ToDateTime() 
            MaturityDate = proto.MaturityDate.ToDateTime() 
            MonthlyPaymentAmount = proto.MonthlyPaymentAmount.DecimalValue
        }
let LeaseType =
    Define.Object<Lease>(
        name = "Lease",
        description = "Lease information",
        fields = [
            Define.AutoField("leaseId", ID)
            Define.AutoField("userId", ID)
            Define.AutoField("startDate", Date)
            Define.AutoField("maturityDate", Date)
            Define.AutoField("monthlyPaymentAmount", Float)
        ]
    )

type LeaseObservation =
    {
        Lease: Lease
        TotalScheduled: decimal
        TotalPaid: decimal
        AmountDue: decimal
        LeaseStatus: LeaseStatus 
    }
module LeaseObservation =
    let fromProto (proto:Tutorial.Lease.V1.LeaseObservation) =
        {
            Lease = proto.Lease |> Lease.fromProto
            TotalScheduled = proto.TotalScheduled.DecimalValue
            TotalPaid = proto.TotalPaid.DecimalValue
            AmountDue = proto.AmountDue.DecimalValue
            LeaseStatus = proto.LeaseStatus |> LeaseStatus.fromProto
        }
let LeaseObservationType =
    Define.Object<LeaseObservation>(
        name = "LeaseObservation",
        description = "Observation of a lease as of a particular date",
        fields = [
            Define.AutoField("lease", LeaseType)
            Define.AutoField("totalScheduled", Float)
            Define.AutoField("totalPaid", Float)
            Define.AutoField("amountDue", Float)
            Define.AutoField("leaseStatus", LeaseStatusType) ])

type LeaseClient(client:Tutorial.Lease.V1.LeaseAPI.LeaseAPIClient) =

    member __.GetLease(leaseId:string, asOfDate:AsOfDate) =
        try
            let req = 
                Tutorial.Lease.V1.GetLeaseRequest( 
                    LeaseId = leaseId, 
                    AsOfDate = (asOfDate |> AsOfDate.toProto))
            let res = client.GetLease(req)
            res.Lease |> LeaseObservation.fromProto
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.NotFound -> failwithf "lease-%s does not exist as of %A" leaseId asOfDate
            | _ -> failwithf "Error!:\n%A" ex
