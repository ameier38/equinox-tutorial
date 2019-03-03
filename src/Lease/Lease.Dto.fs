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
          MonthlyPaymentAmount = schema.MonthlyPaymentAmount |> decimal }
    let fromDomain (lease:Lease) =
        LeaseSchema(
            leaseId = %lease.LeaseId, 
            startDate = lease.StartDate, 
            maturityDate = lease.MaturityDate,
            monthlyPaymentAmount = (lease.MonthlyPaymentAmount |> float32))
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

type ModifiedLeaseSchema = LeaseApiProvider.Schemas.ModifiedLease
module ModifiedLeaseSchema =
    let serializeToJson (schema:ModifiedLeaseSchema) =
        schema.ToJson()
    let deserializeFromBytes (bytes:byte[]) =
        try
            bytes
            |> String.fromBytes
            |> ModifiedLeaseSchema.Parse
            |> Ok
        with ex -> 
            sprintf "could not deserialize ModifiedLeaseSchema:\n%A" ex 
            |> Error
    let toDomain (leaseId:LeaseId) (schema:ModifiedLeaseSchema) =
        { LeaseId = leaseId
          StartDate = schema.StartDate
          MaturityDate = schema.MaturityDate
          MonthlyPaymentAmount = schema.MonthlyPaymentAmount |> decimal }
    let fromDomain (lease:Lease) =
        ModifiedLeaseSchema(
            startDate = lease.StartDate,
            maturityDate = lease.MaturityDate,
            monthlyPaymentAmount = (lease.MonthlyPaymentAmount |> float32))

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
    let fromDomain (payment:Payment) =
        PaymentSchema(
            paymentDate = payment.PaymentDate,
            paymentAmount = (payment.PaymentAmount |> float32))
    let toDomain (schema:PaymentSchema) =
        { PaymentDate = schema.PaymentDate
          PaymentAmount = schema.PaymentAmount |> decimal }

type EventSchema = LeaseApiProvider.Schemas.Event
module EventSchema =
    let create eventType ctx =
        EventSchema(
            eventId = %ctx.EventId,
            eventType = eventType,
            createdDate = %ctx.CreatedDate,
            effectiveDate = %ctx.EffectiveDate)
        |> Some
    let fromDomain = function
        | Undid _ -> None
        | Compacted _ -> None
        | Created { Context = ctx } -> create "Created" ctx
        | Modified { Context = ctx } -> create "Modified" ctx
        | PaymentScheduled { Context = ctx } -> create "PaymentScheduled" ctx
        | PaymentReceived { Context = ctx } -> create "PaymentReceived" ctx
        | LeaseEvent.Terminated ctx -> create "Terminated" ctx

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
            { Lease = schema.Lease |> LeaseSchema.toDomain
              TotalScheduled = schema.TotalScheduled |> decimal
              TotalPaid = schema.TotalPaid |> decimal
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
                lease = (data.Lease |> LeaseSchema.fromDomain),
                status = "Outstanding",
                totalScheduled = (data.TotalScheduled |> float32),
                totalPaid = (data.TotalPaid |> float32),
                amountDue = (data.AmountDue |> float32),
                createdDate = data.CreatedDate,
                updatedDate = data.UpdatedDate,
                events = (events |> List.choose EventSchema.fromDomain))
            |> Ok
        | Terminated data ->
            LeaseStateSchema(
                lease = (data.Lease |> LeaseSchema.fromDomain),
                status = "Terminated",
                totalScheduled = (data.TotalScheduled |> float32),
                totalPaid = (data.TotalPaid |> float32),
                amountDue = (data.AmountDue |> float32),
                createdDate = data.CreatedDate,
                updatedDate = data.UpdatedDate,
                events = (events |> List.choose EventSchema.fromDomain))
            |> Ok
