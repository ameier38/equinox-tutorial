module Graphql.Lease.Fields

open Graphql
open Graphql.Lease.Types
open Graphql.Lease.Client
open FSharp.Data.GraphQL.Types
open Tutorial.Lease.V1

let PageSizeInputType = Define.Input("pageSize", Int)
let PageTokenInputType = Define.Input("pageToken", ID)

let AsOfDateInputType =
    Define.InputObject<AsOfDateInputDto>(
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
        ]
    )

let LeaseStatusType =
    Define.Enum<LeaseStatus>(
        name = "LeaseStatus",
        options = [
            Define.EnumValue("Outstanding", LeaseStatus.Outstanding)
            Define.EnumValue("Terminated", LeaseStatus.Terminated) ],
        description = "Status of the lease"
    )

let LeaseEventType =
    Define.Object<LeaseEvent>(
        name = "LeaseEvent",
        description = "Lease event that has occured",
        fields = [
            Define.AutoField("eventId", Int)
            Define.Field("eventCreatedTime", Date, fun _ e -> e.EventCreatedTime.ToDateTime())
            Define.Field("eventEffectiveDate", Date, fun _ e -> e.EventEffectiveDate.ToDateTime())
            Define.AutoField("eventType", String)
            Define.AutoField("eventPayload", String)
        ]
    )

let ListLeaseEventsResponseType =
    Define.Object<ListLeaseEventsResponse>(
        name = "ListLeaseEventsResponse",
        description = "List lease events repsonse",
        fields = [
            Define.AutoField("events", ListOf LeaseEventType)
            Define.AutoField("prevPageToken", String)
            Define.AutoField("nextPageToken", String)
            Define.AutoField("totalCount", Int)
        ]
    )

let LeaseType =
    Define.Object<Lease>(
        name = "Lease",
        description = "Lease information",
        fields = [
            Define.AutoField("leaseId", ID)
            Define.AutoField("userId", ID)
            Define.Field("startDate", Date, fun _ lease -> lease.StartDate.ToDateTime())
            Define.Field("maturityDate", Date, fun _ lease -> lease.MaturityDate.ToDateTime())
            Define.Field("monthlyPaymentAmount", Float, fun _ lease -> lease.MonthlyPaymentAmount.DecimalValue |> float)
        ]
    )

let LeaseObservationType 
    (leaseClient:LeaseClient) =
    Define.Object<LeaseObservation>(
        name = "LeaseObservation",
        description = "Observation of a lease as of a particular date",
        fields = [
            Define.AutoField("lease", LeaseType)
            Define.Field("totalScheduled", Float, fun _ obs -> obs.TotalScheduled.DecimalValue |> float)
            Define.Field("totalPaid", Float, fun _ obs -> obs.TotalPaid.DecimalValue |> float)
            Define.Field("amountDue", Float, fun _ obs -> obs.AmountDue.DecimalValue |> float)
            Define.AutoField("leaseStatus", LeaseStatusType)
            Define.Field(
                name = "listEvents", 
                typedef = ListLeaseEventsResponseType, 
                args = [
                    PageSizeInputType
                    PageTokenInputType
                ],
                resolve = (fun ctx leaseObs ->
                    let leaseId = leaseObs.Lease.LeaseId
                    let asOfDate = leaseObs.AsOfDate
                    let pageSize = ctx.TryArg("pageSize") |> Option.defaultValue 20
                    let pageToken = ctx.TryArg("pageToken") |> Option.defaultValue ""
                    leaseClient.ListLeaseEvents(leaseId, asOfDate, pageSize, pageToken
                )
            ))
        ]
    )

let GetLeaseInputType =
    Define.InputObject<GetLeaseInputDto>(
        name = "GetLeaseInput",
        fields = [
            Define.Input("leaseId", ID)
            Define.Input("asOfDate", Nullable AsOfDateInputType) 
        ]
    )

let getLeaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "getLease",
        typedef = (LeaseObservationType leaseClient),
        description = "get a lease at a point in time",
        args = [ 
        ],
        resolve = (fun ctx _ ->
            let leaseId = ctx.Arg("leaseId")
            let asOfDate = 
                ctx.TryArg<AsOfDateInput>("asOfDate")
                |> Option.map AsOfDate.fromInput
                |> Option.defaultValue (AsOfDate.getDefault())
            leaseClient.GetLease(leaseId, asOfDate)
        )
    )

let ListLeasesResponseType =
    Define.Object<ListLeasesResponse>(
        name = "ListLeasesResponse",
        description = "List leases response",
        fields = [
            Define.AutoField("leases", ListOf LeaseType)
            Define.AutoField("nextPageToken", String)
            Define.AutoField("totalCount", Int)
        ]
    )

let listLeasesField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "listLeases",
        typedef = ListLeasesResponseType,
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

let CreateLeaseInputType =
    Define.InputObject<CreateLeaseInputDto>(
        name = "CreateLeaseInput",
        description = "Lease",
        fields = [
            Define.Input(
                name = "leaseId",
                typedef = ID,
                description = "Unique identifier of the lease")
            Define.Input(
                name = "userId",
                typedef = ID,
                description = "Unique identifier of the user")
            Define.Input(
                name = "startDate",
                typedef = Date,
                description = "Start date of the lease")
            Define.Input(
                name = "maturityDate",
                typedef = Date,
                description = "Maturity date of the lease")
            Define.Input(
                name = "monthlyPaymentAmount",
                typedef = Float,
                description = "Monthly payment amount for the lease")
        ]
    )

let createLeaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "createLease",
        typedef = String,
        description = "create a new lease",
        args = [ Define.Input("input", CreateLeaseInputType) ],
        resolve = (fun ctx _ ->
            let createLeaseInputDto = ctx.Arg<CreateLeaseInputDto>("input")
            leaseClient.CreateLease(createLeaseInputDto)
        )
    )

let schedulePaymentField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "schedulePayment",
        typedef = String,
        description = "schedule a payment",
        args = [
            Define.Input("leaseId", ID)
            Define.Input("paymentId", ID)
            Define.Input("paymentDate", Date)
            Define.Input("paymentAmount", Float)
        ],
        resolve = (fun ctx _ ->
            let leaseId = ctx.Arg("leaseId")
            printfn "leaseId: %s" leaseId
            let paymentId = ctx.Arg("paymentId")
            printfn "paymentId: %s" paymentId
            let paymentDate = ctx.Arg("paymentDate") |> DateTime.parse |> DateTime.toUtc
            printfn "paymentDate: %A" paymentDate
            let paymentAmount = ctx.Arg("paymentAmount")
            printfn "paymentAmount: %A" paymentAmount
            leaseClient.SchedulePayment(leaseId, paymentId, paymentDate, paymentAmount))
    )

let receivePaymentField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "receivePayment",
        typedef = String,
        description = "receive a payment",
        args = [
            Define.Input("leaseId", ID)
            Define.Input("paymentId", ID)
            Define.Input("paymentDate", Date)
            Define.Input("paymentAmount", Float)
        ],
        resolve = (fun ctx _ ->
            let leaseId = ctx.Arg("leaseId")
            let paymentId = ctx.Arg("paymentId")
            let paymentDate = ctx.Arg("paymentDate") |> DateTime.parse |> DateTime.toUtc
            let paymentAmount = ctx.Arg("paymentAmount")
            leaseClient.ReceivePayment(leaseId, paymentId, paymentDate, paymentAmount))
    )

let terminateLeaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "terminateLease",
        typedef = String,
        description = "terminate a lease",
        args = [
            Define.Input("leaseId", ID)
            Define.Input("effectiveDate", Date)
        ],
        resolve = (fun ctx _ ->
            let leaseId = ctx.Arg("leaseId")
            let effDate = ctx.Arg("effectiveDate")
            leaseClient.TerminateLease(leaseId, effDate))
    )

let deleteLeaseEventField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "deleteLeaseEvent",
        typedef = String,
        description = "delete a lease event",
        args = [
            Define.Input("leaseId", ID)
            Define.Input("eventId", Int)
        ],
        resolve = (fun ctx _ ->
            let leaseId = ctx.Arg("leaseId")
            let eventId = ctx.Arg("eventId")
            leaseClient.DeleteEvent(leaseId, eventId))
    )
