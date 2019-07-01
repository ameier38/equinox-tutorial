module Graphql.Lease.Fields

open Graphql
open Graphql.Lease.Client
open FSharp.Data.GraphQL.Types

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
        ]
    )

let LeaseStatusType =
    Define.Enum(
        name = "LeaseStatus",
        options = [
            Define.EnumValue("Outstanding", Outstanding)
            Define.EnumValue("Terminated", Terminated) ],
        description = "Status of the lease"
    )

let LeaseEventType =
    Define.Object<LeaseEvent>(
        name = "LeaseEvent",
        description = "Lease event that has occured",
        fields = [
            Define.Field("leaseId", Int, fun _ event -> event.EventId)
            Define.AutoField("eventCreatedTime", Date)
            Define.AutoField("eventEffectiveDate", Date)
            Define.AutoField("eventType", String)
        ]
    )

let ListLeaseEventsResponseType =
    Define.Object<ListLeaseEventsResponse>(
        name = "ListLeaseEventsResponse",
        description = "List lease events repsonse",
        fields = [
            Define.AutoField("events", ListOf LeaseEventType)
            Define.AutoField("nextPageToken", String)
            Define.AutoField("totalCount", Int)
        ]
    )

let LeaseType =
    Define.Object<Lease>(
        name = "Lease",
        description = "Lease information",
        fields = [
            Define.Field("leaseId", ID, fun _ lease -> lease.LeaseId)
            Define.AutoField("userId", ID)
            Define.AutoField("startDate", Date)
            Define.AutoField("maturityDate", Date)
            Define.AutoField("monthlyPaymentAmount", Float)
        ]
    )

let LeaseObservationType 
    (leaseClient:LeaseClient) =
    Define.Object<LeaseObservation>(
        name = "LeaseObservation",
        description = "Observation of a lease as of a particular date",
        fields = [
            Define.AutoField("lease", LeaseType)
            Define.AutoField("totalScheduled", Float)
            Define.AutoField("totalPaid", Float)
            Define.AutoField("amountDue", Float)
            Define.AutoField("leaseStatus", LeaseStatusType) 
            Define.Field("listEvents", ListLeaseEventsResponseType, (fun ctx leaseObs ->
                let leaseId = leaseObs.Lease.LeaseId
                let asOfDate = 
                    ctx.TryArg<AsOfDateInput>("asOfDate")
                    |> Option.map AsOfDate.fromInput
                    |> Option.defaultValue (AsOfDate.getDefault())
                let pageSize = ctx.TryArg("pageSize") |> Option.defaultValue 20
                let pageToken = ctx.TryArg("pageToken") |> Option.defaultValue ""
                leaseClient.ListLeaseEvents(leaseId, asOfDate, pageSize, pageToken)
            ))
        ]
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

let getLeaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "getLease",
        typedef = (LeaseObservationType leaseClient),
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
            leaseClient.GetLease(leaseId, asOfDate)
        )
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

let createLeaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "createLease",
        typedef = String,
        description = "create a new lease",
        args = [
            Define.Input("leaseId", ID)
            Define.Input("userId", ID)
            Define.Input("startDate", Date)
            Define.Input("maturityDate", Date)
            Define.Input("monthlyPaymentAmount", Float)
        ],
        resolve = (fun ctx _ ->
            let leaseId = ctx.Arg("leaseId")
            let userId = ctx.Arg("userId")
            let startDate = ctx.Arg("startDate") |> DateTime.parse |> DateTime.toUtc
            let maturityDate = ctx.Arg("maturityDate") |> DateTime.parse |> DateTime.toUtc
            let monthlyPaymentAmount = ctx.Arg("monthlyPaymentAmount")
            leaseClient.CreateLease(leaseId, userId, startDate, maturityDate, monthlyPaymentAmount)
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
            let paymentId = ctx.Arg("paymentId")
            let paymentDate = ctx.Arg("paymentDate")
            let paymentAmount = ctx.Arg("paymentAmount")
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
            let paymentDate = ctx.Arg("paymentDate")
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
