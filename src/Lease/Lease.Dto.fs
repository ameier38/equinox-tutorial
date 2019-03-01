namespace Lease

open OpenAPITypeProvider
open FSharp.UMX

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
    let serializeToJson (schema:NewLeaseSchema) =
        schema.ToJson()
    let fromDomain (newLease:NewLease) =
        NewLeaseSchema(
            startDate = newLease.StartDate,
            maturityDate = newLease.MaturityDate,
            monthlyPaymentAmount = (newLease.MonthlyPaymentAmount |> float32))
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
    let deserializeFromBytes (bytes:byte[]) =
        try
            bytes
            |> String.fromBytes
            |> PaymentSchema.Parse
            |> Ok
        with ex -> 
            sprintf "could not deserialize NewLeaseSchema:\n%A" ex 
            |> Error
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
        | Created { Context = ctx } -> create "Created" ctx
        | Modified { Context = ctx } -> create "Modified" ctx
        | PaymentScheduled { Context = ctx } -> create "PaymentScheduled" ctx
        | PaymentReceived { Context = ctx } -> create "PaymentReceived" ctx
        | LeaseEvent.Terminated ctx -> create "Terminated" ctx

type LeaseStateSchema = OpenApi.LeaseApi.Schemas.LeaseState
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
              AmountDue = schema.AmountDue |> decimal }
        match schema.Status with
        | s when s = "Outstanding" -> Outstanding stateData
        | s when s = "Terminated" -> Terminated stateData
        | _ -> Corrupt "invalid state"
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
