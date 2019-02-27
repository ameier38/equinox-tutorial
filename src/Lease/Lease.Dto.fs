namespace Lease

open OpenAPITypeProvider
open FSharp.UMX
open Ouroboros
open System.Text

module OpenApi =
    let [<Literal>] LeaseApiSchema = __SOURCE_DIRECTORY__  + "/openapi.yaml"
    type LeaseApi = OpenAPIV3Provider<LeaseApiSchema>

type NewLeaseSchema = OpenApi.LeaseApi.Schemas.NewLease
module NewLeaseSchema =
    let deserializeFromBytes (bytes:byte[]) =
        try
            bytes
            |> String.fromBytes
            |> NewLeaseSchema.Parse
            |> Ok
        with ex -> 
            sprintf "could not deserialize NewLeaseSchema:\n%A" ex 
            |> Error
    let toDomain (schema:NewLeaseSchema) =
        { StartDate = schema.StartDate
          MaturityDate = schema.MaturityDate
          MonthlyPaymentAmount = schema.MonthlyPaymentAmount |> decimal }

type LeaseSchema = OpenApi.LeaseApi.Schemas.Lease
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

type PaymentSchema = OpenApi.LeaseApi.Schemas.Payment
module PaymentSchema =
    let toDomain (schema:PaymentSchema) =
        { PaymentDate = schema.PaymentDate
          PaymentAmount = schema.PaymentAmount |> decimal }

type EventSchema = OpenApi.LeaseApi.Schemas.Event
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
        | Created (_, ctx) -> create "Created" ctx
        | Modified (_, ctx) -> create "Modified" ctx
        | PaymentScheduled (_, ctx) -> create "PaymentScheduled" ctx
        | PaymentReceived (_, ctx) -> create "PaymentReceived" ctx
        | LeaseEvent.Terminated ctx -> create "Terminated" ctx

type LeaseStateSchema = OpenApi.LeaseApi.Schemas.LeaseState
module LeaseStateSchema =
    let serializeToJson (schema:LeaseStateSchema) =
        schema.ToJson()
    let serializeToBytes (schema:LeaseStateSchema) = 
        schema
        |> serializeToJson
        |> String.toBytes
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
                events = (events |> List.choose EventSchema.fromDomain))
            |> Ok
        | Terminated data ->
            LeaseStateSchema(
                lease = (data.Lease |> LeaseSchema.fromDomain),
                status = "Outstanding",
                totalScheduled = (data.TotalScheduled |> float32),
                totalPaid = (data.TotalPaid |> float32),
                amountDue = (data.AmountDue |> float32),
                events = (events |> List.choose EventSchema.fromDomain))
            |> Ok
