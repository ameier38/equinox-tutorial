module Lease.Dto

open OpenAPITypeProvider
open FSharp.UMX

let [<Literal>] LeaseApiSchemaPath = __SOURCE_DIRECTORY__  + "/openapi.yaml"
type LeaseApiProvider = OpenAPIV3Provider<LeaseApiSchemaPath>

type LeaseSchema = LeaseApiProvider.Schemas.Lease
module LeaseSchema =
    let toDomain (schema:LeaseSchema) =
        { LeaseId = %schema.LeaseId
          StartDate = schema.StartDate
          MaturityDate = schema.MaturityDate
          MonthlyPaymentAmount = schema.MonthlyPaymentAmount |> decimal |> UMX.tag<monthlyPaymentAmount> }
    let fromDomain (lease:Lease) =
        LeaseSchema(
            leaseId = %lease.LeaseId, 
            startDate = lease.StartDate, 
            maturityDate = lease.MaturityDate,
            monthlyPaymentAmount = (lease.MonthlyPaymentAmount |> UMX.untag |> float32))
    let deserializeFromBytes (bytes:byte[]) =
        try
            bytes
            |> String.fromBytes
            |> LeaseSchema.Parse
            |> Ok
        with ex -> 
            sprintf "could not deserialize LeaseSchema:\n%A" ex 
            |> Error
    let serializeToJson (schema:LeaseSchema) =
        schema.ToJson()

type PaymentSchema = LeaseApiProvider.Schemas.Payment
module PaymentSchema =
    let deserializeFromBytes (bytes:byte[]) =
        try
            bytes
            |> String.fromBytes
            |> PaymentSchema.Parse
            |> Ok
        with ex -> 
            sprintf "could not deserialize PaymentSchema:\n%A" ex 
            |> Error
    let serializeToJson (schema:PaymentSchema) =
        schema.ToJson()
    let fromScheduledPayment (pmt:ScheduledPayment) =
        PaymentSchema(
            paymentDate = %pmt.ScheduledPaymentDate,
            paymentAmount = (pmt.ScheduledPaymentAmount |> UMX.untag |> float32))
    let fromPayment (pmt:Payment) =
        PaymentSchema(
            paymentDate = %pmt.PaymentDate,
            paymentAmount = (pmt.PaymentAmount |> UMX.untag |> float32))
    let toScheduledPayment (schema:PaymentSchema) =
        { ScheduledPaymentDate = %schema.PaymentDate
          ScheduledPaymentAmount = schema.PaymentAmount |> decimal |> UMX.tag<scheduledPaymentAmount> }
    let toPayment (schema:PaymentSchema) =
        { PaymentDate = %schema.PaymentDate
          PaymentAmount = schema.PaymentAmount |> decimal |> UMX.tag<paymentAmount> }

type EventSchema = LeaseApiProvider.Schemas.Event
module EventSchema =
    let create eventType ctx =
        EventSchema(
            eventId = %ctx.EventId,
            eventType = eventType,
            createdDate = %ctx.EventCreatedDate,
            effectiveDate = %ctx.EventEffectiveDate)
        |> Some
    let fromDomain = function
        | Undid _ -> None
        | Created e -> create "Created" e.Context
        | PaymentScheduled e -> create "PaymentScheduled" e.Context
        | PaymentReceived e -> create "PaymentReceived" e.Context
        | LeaseEvent.Terminated e -> create "Terminated" e.Context

type LeaseStateSchema = LeaseApiProvider.Schemas.LeaseState
module LeaseStateSchema =
    let serializeToJson (schema:LeaseStateSchema) =
        schema.ToJson()
    let serializeToBytes (schema:LeaseStateSchema) = 
        schema
        |> serializeToJson
        |> String.toBytes
    let deserializeFromBytes (bytes:byte[]) =
        bytes
        |> String.fromBytes
        |> LeaseStateSchema.Parse
    let toDomain (schema:LeaseStateSchema) =
        let stateData =
            { NextId = %schema.NextId
              Lease = schema.Lease |> LeaseSchema.toDomain
              TotalScheduled = schema.TotalScheduled |> decimal |> UMX.tag<scheduledPaymentAmount>
              TotalPaid = schema.TotalPaid |> decimal |> UMX.tag<paymentAmount>
              AmountDue = schema.AmountDue |> decimal
              CreatedDate = schema.CreatedDate
              UpdatedDate = schema.UpdatedDate }
        match schema.Status with
        | s when s = "Outstanding" -> Outstanding stateData |> Ok
        | s when s = "Terminated" -> Terminated stateData |> Ok
        | s -> sprintf "cannot convert to domain in state %s" s |> Error
    let fromDomain (state:LeaseState, events: LeaseEvent list) =
        match state with
        | NonExistent -> Error "lease does not exist"
        | Corrupt err -> Error err
        | Outstanding data ->
            LeaseStateSchema(
                nextId = %data.NextId,
                lease = (data.Lease |> LeaseSchema.fromDomain),
                status = "Outstanding",
                totalScheduled = (data.TotalScheduled |> UMX.untag |> float32),
                totalPaid = (data.TotalPaid |> UMX.untag |> float32),
                amountDue = (data.AmountDue |> float32),
                createdDate = data.CreatedDate,
                updatedDate = data.UpdatedDate,
                events = (events |> List.choose EventSchema.fromDomain))
            |> Ok
        | Terminated data ->
            LeaseStateSchema(
                nextId = %data.NextId,
                lease = (data.Lease |> LeaseSchema.fromDomain),
                status = "Terminated",
                totalScheduled = (data.TotalScheduled |> UMX.untag |> float32),
                totalPaid = (data.TotalPaid |> UMX.untag |> float32),
                amountDue = (data.AmountDue |> float32),
                createdDate = data.CreatedDate,
                updatedDate = data.UpdatedDate,
                events = (events |> List.choose EventSchema.fromDomain))
            |> Ok
