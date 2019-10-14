module Lease.Fields

open Lease
open Lease.Types
open Lease.Client
open FSharp.Data.GraphQL.Types
open Tutorial.Lease.V1

let leaseIdInputField = 
    Define.Input(
        name = "leaseId", 
        typedef = ID,
        description = "Unique identifier of a lease")
let pageSizeInputField = 
    Define.Input(
        name = "pageSize", 
        typedef = Nullable Int,
        description = "Maximum number of items in a page")
let pageTokenInputField = 
    Define.Input(
        name = "pageToken", 
        typedef = Nullable ID,
        description = "Token for page to retrieve; Empty string for first page")

let AsOfInputObject =
    Define.InputObject<AsOfInputDto>(
        name = "AsOf",
        description = "Point in time on and at which to observe state",
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

let asOfInputField = 
    Define.Input(
        name = "asOf", 
        typedef = (Nullable AsOfInputObject),
        description = "Point in time on and at which to observe state")

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

let LeaseType =
    Define.Object<Lease>(
        name = "Lease",
        description = "Static information for a lease",
        fields = [
            Define.AutoField("leaseId", ID)
            Define.AutoField("userId", ID)
            Define.Field("commencementDate", Date, fun _ lease -> lease.CommencementDate.ToDateTime())
            Define.Field("expirationDate", Date, fun _ lease -> lease.ExpirationDate.ToDateTime())
            Define.Field("monthlyPaymentAmount", Float, fun _ lease -> lease.MonthlyPaymentAmount.DecimalValue |> float)
        ]
    )

let LeaseObservationType =
    Define.Object<LeaseObservation>(
        name = "LeaseObservation",
        description = "Observation of a lease as of a particular date",
        fields = [
            Define.Field("createdAtTime", Date, fun _ obs -> obs.CreatedAtTime.ToDateTime())
            Define.Field("updatedAtTime", Date, fun _ obs -> obs.UpdatedAtTime.ToDateTime())
            Define.Field("updatedOnDate", Date, fun _ obs -> obs.UpdatedOnDate.ToDateTime())
            Define.AutoField("leaseId", ID)
            Define.AutoField("userId", ID)
            Define.Field("commencementDate", Date, fun _ lease -> lease.CommencementDate.ToDateTime())
            Define.Field("expirationDate", Date, fun _ lease -> lease.ExpirationDate.ToDateTime())
            Define.Field("monthlyPaymentAmount", Float, fun _ lease -> lease.MonthlyPaymentAmount.DecimalValue |> float)
            Define.Field("totalScheduled", Float, fun _ obs -> obs.TotalScheduled.DecimalValue |> float)
            Define.Field("totalPaid", Float, fun _ obs -> obs.TotalPaid.DecimalValue |> float)
            Define.Field("amountDue", Float, fun _ obs -> obs.AmountDue.DecimalValue |> float)
            Define.AutoField("leaseStatus", LeaseStatusType)
        ]
    )

let ListLeaseEventsInputObject =
    Define.InputObject<ListLeaseEventsInputDto>(
        name = "ListLeaseEventsInput",
        description = "Input for listing lease events",
        fields = [
            leaseIdInputField
            asOfInputField
            pageSizeInputField
            pageTokenInputField
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

let listLeaseEventsField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "listLeaseEvents",
        typedef = ListLeaseEventsResponseType,
        args = [Define.Input("input", ListLeaseEventsInputObject)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<ListLeaseEventsInputDto>("input")
            leaseClient.ListLeaseEvents(input)
        )
    )

let GetLeaseInputObject =
    Define.InputObject<GetLeaseInputDto>(
        name = "GetLeaseInput",
        fields = [leaseIdInputField; asOfInputField]
    )

let getLeaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "getLease",
        typedef = LeaseObservationType,
        description = "Get a lease at a point in time",
        args = [Define.Input("input", GetLeaseInputObject)],
        resolve = (fun ctx _ ->
            let getLeaseInput = ctx.Arg<GetLeaseInputDto>("input")
            leaseClient.GetLease(getLeaseInput)
        )
    )

let ListLeasesInputObject =
    Define.InputObject<ListLeasesInputDto>(
        name = "ListLeasesInput",
        fields = [
            pageSizeInputField
            pageTokenInputField
        ]
    )

let ListLeasesResponseType =
    Define.Object<ListLeasesResponse>(
        name = "ListLeasesResponse",
        description = "List leases response",
        fields = [
            Define.AutoField("leases", ListOf LeaseType)
            Define.AutoField("prevPageToken", String)
            Define.AutoField("nextPageToken", String)
            Define.AutoField("totalCount", Int)
        ]
    )

let listLeasesField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "listLeases",
        typedef = ListLeasesResponseType,
        description = "List existing leases",
        args = [Define.Input("input", ListLeasesInputObject)],
        resolve = (fun ctx _ ->
            let listLeasesInput = ctx.Arg<ListLeasesInputDto>("input")
            leaseClient.ListLeases(listLeasesInput)
        )
    )

let CreateLeaseInputObject =
    Define.InputObject<CreateLeaseInputDto>(
        name = "CreateLeaseInput",
        description = "Input for creating a lease",
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
                name = "commencementDate",
                typedef = Date,
                description = "Date on which the lease begins")
            Define.Input(
                name = "expirationDate",
                typedef = Date,
                description = "Date on which the lease ends")
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
        description = "Create a new lease",
        args = [Define.Input("input", CreateLeaseInputObject)],
        resolve = (fun ctx _ ->
            let createLeaseInputDto = ctx.Arg<CreateLeaseInputDto>("input")
            leaseClient.CreateLease(createLeaseInputDto)
        )
    )

let SchedulePaymentInputObject =
    Define.InputObject<SchedulePaymentInputDto>(
        name = "SchedulePaymentInput",
        description = "Input for scheduling a payment",
        fields = [
            leaseIdInputField
            Define.Input("paymentId", ID)
            Define.Input("scheduledDate", Date)
            Define.Input("scheduledAmount", Float)
        ]
    )

let schedulePaymentField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "schedulePayment",
        typedef = String,
        description = "Schedule a payment",
        args = [Define.Input("input", SchedulePaymentInputObject)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<SchedulePaymentInputDto>("input")
            leaseClient.SchedulePayment(input))
    )

let ReceivePaymentInputObject =
    Define.InputObject<ReceivePaymentInputDto>(
        name = "ReceivePaymentInput",
        description = "Input for receiving a payment",
        fields = [
            leaseIdInputField
            Define.Input("paymentId", ID)
            Define.Input("receivedDate", Date)
            Define.Input("receivedAmount", Float)
        ]
    )

let receivePaymentField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "receivePayment",
        typedef = String,
        description = "receive a payment",
        args = [Define.Input("input", ReceivePaymentInputObject)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<ReceivePaymentInputDto>("input")
            leaseClient.ReceivePayment(input))
    )

let TerminateLeaseInputObject =
    Define.InputObject<TerminateLeaseInputDto>(
        name = "TerminateLeaseInput",
        description = "Input for terminating a lease",
        fields = [
            leaseIdInputField
            Define.Input("terminationDate", Date)
            Define.Input("terminationReason", String)
        ]
    )

let terminateLeaseField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "terminateLease",
        typedef = String,
        description = "terminate a lease",
        args = [Define.Input("input", TerminateLeaseInputObject)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<TerminateLeaseInputDto>("input")
            leaseClient.TerminateLease(input))
    )

let DeleteLeaseEventInputObject =
    Define.InputObject<DeleteLeaseEventInputDto>(
        name = "DeleteLeaseEventInput",
        description = "Input for deleting a lease event",
        fields = [
            leaseIdInputField
            Define.Input("eventId", Int)            
        ]
    )

let deleteLeaseEventField
    (leaseClient:LeaseClient) =
    Define.Field(
        name = "deleteLeaseEvent",
        typedef = String,
        description = "delete a lease event",
        args = [Define.Input("input", DeleteLeaseEventInputObject)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<DeleteLeaseEventInputDto>("input")
            leaseClient.DeleteLeaseEvent(input))
    )
