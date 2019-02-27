namespace Lease

open OpenAPITypeProvider
open FSharp.UMX
open Ouroboros
open System.Text

module OpenApi =
    let [<Literal>] LeaseApiSchema = __SOURCE_DIRECTORY__  + "/openapi.yaml"
    type LeaseApi = OpenAPIV3Provider<LeaseApiSchema>

module String =
    let toBytes (s:string) = s |> Encoding.UTF8.GetBytes
    let fromBytes (bytes:byte []) = bytes |> Encoding.UTF8.GetString

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

type LeaseStateSchema = OpenApi.LeaseApi.Schemas.LeaseState
module LeaseStateSchema =
    let serializeToJson (schema:LeaseStateSchema) =
        schema.ToJson()
    let serializeToBytes (schema:LeaseStateSchema) = 
        schema
        |> serializeToJson
        |> String.toBytes

type UndoRequestSchema = OpenApi.LeaseApi.Schemas.UndoRequest
module UndoRequestSchema =
    let deserializeFromBytes (bytes:byte []) =
        try
            bytes
            |> String.fromBytes
            |> UndoRequestSchema.Parse
            |> Ok
        with 
        | ex ->
            sprintf "could not parse UndoRequestSchema %A" ex
            |> Error
    let toDomain (schema:UndoRequestSchema) : LeaseId * EventId =
        (%schema.LeaseId, %schema.EventId)

type PlayRequestSchema = OpenApi.DogApi.Schemas.PlayRequest
module PlayRequestSchema =
    let deserializeFromBytes (bytes:byte []) =
        try
            bytes
            |> String.fromBytes
            |> PlayRequestSchema.Parse
            |> Ok
        with 
        | ex ->
            sprintf "could not parse PlayRequestSchema %A" ex
            |> DogError
            |> Error
    let toDomain (schema:PlayRequestSchema) =
        let dogId = 
            schema.DogId 
            |> DogId.fromGuid
        let envelope = { EffectiveDate = schema.EffectiveDate}
        (dogId, envelope)

type SleepRequestSchema = OpenApi.DogApi.Schemas.SleepRequest
module SleepRequestSchema =
    let deserializeFromBytes (bytes:byte []) =
        try
            bytes
            |> String.fromBytes
            |> SleepRequestSchema.Parse
            |> Ok
        with 
        | ex ->
            sprintf "could not parse SleepRequestSchema %A" ex
            |> DogError
            |> Error
    let toDomain (schema: SleepRequestSchema) =
        let dogId = 
            schema.DogId 
            |> DogId.fromGuid
        let envelop =
            { EffectiveDate = schema.EffectiveDate
              Data = schema.TimeSpan |> float |> TimeSpan.FromHours }
        (dogId, envelop)

type EatRequestSchema = OpenApi.DogApi.Schemas.EatRequest
module EatRequestSchema =
    let deserializeFromBytes (bytes:byte []) =
        try
            bytes
            |> String.fromBytes
            |> SleepRequestSchema.Parse
            |> Ok
        with 
        | ex ->
            sprintf "could not parse EatRequestSchema %A" ex
            |> DogError
            |> Error
    let toDomain (schema: EatRequestSchema) =
        let dogId = 
            schema.DogId 
            |> DogId.fromGuid
        let envelop =
            { EffectiveDate = schema.EffectiveDate
              Data = schema.Weight |> decimal |> Grams.fromDecimal |> Weight.create }
        (dogId, envelop)

type GetRequestSchema = OpenApi.DogApi.Schemas.GetRequest
module GetRequestSchema =
    let deserializeFromBytes (bytes:byte []) = 
        try
            bytes
            |> String.fromBytes
            |> GetRequestSchema.Parse
            |> Ok
        with 
        | ex ->
            sprintf "could not parse GetRequestSchema %A" ex
            |> DogError
            |> Error
    let (|Of|At|Invalid|) obsType =
        match obsType with
        | s when (s |> String.lower) = "of" -> Of
        | s when (s |> String.lower) = "at" -> At
        | _ -> Invalid
    let toDomain (schema:GetRequestSchema) =
        result {
            let dogId = 
                schema.DogId 
                |> DogId.fromGuid
            let! obsDate = 
                match schema.ObservationType with 
                | Of -> 
                    schema.ObservationDate 
                    |> AsOf 
                    |> Ok
                | At ->
                    schema.ObservationDate 
                    |> AsAt 
                    |> Ok
                | Invalid as obsType ->
                    sprintf "%s is not a valid observation type; options are 'as' or 'of'" obsType
                    |> DogError
                    |> Error
            return (dogId, obsDate)
        }
