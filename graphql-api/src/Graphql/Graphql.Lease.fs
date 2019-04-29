module Graphql.Lease

open FSharp.Data.GraphQL.Types

type AsOfDate =
    { AsAt: System.DateTime
      AsOn: System.DateTime }
module AsOfDate =
    let toProto (asOfDate:AsOfDate) =
        Proto.Lease.AsOfDate(
            AsAt = (asOfDate.AsAt |> DateTime.toTimestamp),
            AsOn = (asOfDate.AsOn |> DateTime.toTimestamp))

type AsOfDateInput =
    { AsAt: System.DateTime option
      AsOn: System.DateTime option }
module AsOfDateInput =
    let toAsOfDate (asOfDateInputOpt:AsOfDateInput option) =
        let utcNow = System.DateTime.UtcNow
        let defaultAsOfDate =
            { AsOfDate.AsAt = utcNow
              AsOn = utcNow }
        asOfDateInputOpt
        |> Option.map (fun d ->
            { AsOfDate.AsAt = d.AsAt |> Option.defaultValue utcNow
              AsOn = d.AsOn |> Option.defaultValue utcNow })
        |> Option.defaultValue defaultAsOfDate
let AsOfDateInputType =
    Define.InputObject<AsOfDateInput>(
        name = "AsOfDate",
        description = "As of date",
        fields = [
            Define.Input(
                name = "asAt",
                typedef = String,
                description = "Filter events created at or before this date")
            Define.Input(
                name = "asOn",
                typedef = String,
                description = "Filter events effective on or before this date") ])

type LeaseStatus =
    | Outstanding
    | Terminated
module LeaseStatus =
    let fromProto (proto:Proto.Lease.LeaseStatus) =
        match proto with
        | Proto.Lease.LeaseStatus.Outstanding -> Outstanding |> Ok
        | Proto.Lease.LeaseStatus.Terminated -> Terminated |> Ok
        | other -> sprintf "invalid LeaseStatus %A" other |> Error
let LeaseStatusType =
    Define.Enum(
        name = "LeaseStatus",
        options = [
            Define.EnumValue("Outstanding", Outstanding)
            Define.EnumValue("Terminated", Terminated) ],
        description = "Status of the lease")

type LeaseObservation =
    { ObservationDate: System.DateTime
      LeaseId: string
      StartDate: System.DateTime
      MaturityDate: System.DateTime
      MonthlyPaymentAmount: float32
      TotalScheduled: float32
      TotalPaid: float32
      AmountDue: float32
      LeaseStatus: LeaseStatus }
module LeaseObservation =
    let fromProto (proto:Proto.Lease.LeaseObservation) =
        result {
            let! leaseStatus = proto.LeaseStatus |> LeaseStatus.fromProto
            return
                { ObservationDate = proto.ObservationDate.ToDateTime()
                  LeaseId = proto.LeaseId
                  StartDate = proto.StartDate.ToDateTime()
                  MaturityDate = proto.MaturityDate.ToDateTime()
                  MonthlyPaymentAmount = proto.MonthlyPaymentAmount
                  TotalScheduled = proto.TotalScheduled
                  TotalPaid = proto.TotalPaid
                  AmountDue = proto.AmountDue
                  LeaseStatus = leaseStatus }
        }
let LeaseObservationType =
    Define.Object<LeaseObservation>(
        name = "LeaseObservation",
        description = "Observation of a lease as of a particular date",
        fields = [
            Define.AutoField("observationDate", Date)
            Define.AutoField("loanId", ID)
            Define.AutoField("startDate", Date)
            Define.AutoField("maturityDate", Date)
            Define.AutoField("monthlyPaymentAmount", Float)
            Define.AutoField("totalScheduled", Float)
            Define.AutoField("totalPaid", Float)
            Define.AutoField("amountDue", Float)
            Define.Field("leaseStatus", LeaseStatusType, fun _ obs -> obs.LeaseStatus) ])

type LeaseService(client:Proto.Lease.LeaseService.LeaseServiceClient) =

    member __.GetLease(leaseId:string, asOfDateInputOpt:AsOfDateInput option) =
        let asOfDate = 
            asOfDateInputOpt 
            |> AsOfDateInput.toAsOfDate
            |> AsOfDate.toProto
        let qry = Proto.Lease.Query(LeaseId = leaseId, AsOfDate = asOfDate)
        let res = client.QueryState(qry)
        match res.ResponseCase with
        | Proto.Lease.QueryStateResponse.ResponseOneofCase.Ok ->
            let state = res.Ok
            match state.StateCase with
            | Proto.Lease.QueryStateResponse.Types.LeaseState.StateOneofCase.Observation ->
                state.Observation |> LeaseObservation.fromProto
            | Proto.Lease.QueryStateResponse.Types.LeaseState.StateOneofCase.None ->
                sprintf "lease %s does not exist as of %A" leaseId asOfDate |> Error
            | other -> sprintf "invalid LeaseState %A" other |> Error
        | Proto.Lease.QueryStateResponse.ResponseOneofCase.Error ->
            res.Error |> Error
        | other -> sprintf "invalid QueryStateResponse %A" other |> Error
                