namespace Dog

open OpenAPITypeProvider
open System

module OpenApi =
    let [<Literal>] DogApiSchema = __SOURCE_DIRECTORY__  + "/openapi.yaml"
    type DogApi = OpenAPIV3Provider<DogApiSchema>

type DogSchema = OpenApi.DogApi.Schemas.Dog
module DogSchema =
    let toDomain (schema:DogSchema) =
        result {
            let! name = schema.Name |> Name.create
            let! breed = schema.Breed |> Breed.create
            return
                { Name = name
                  Breed = breed
                  BirthDate = schema.BirthDate }
        }
    let fromDomain (dog:Dog) =
        DogSchema(
            name = (dog.Name |> Name.value), 
            breed = (dog.Breed |> Breed.value), 
            birthDate = dog.BirthDate)

type DogStateSchema = OpenApi.DogApi.Schemas.DogState
module DogStateSchema =
    let serializeToJson (schema:DogStateSchema) =
        schema.ToJson()
    let serializeToBytes (schema:DogStateSchema) = 
        schema
        |> serializeToJson
        |> String.toBytes

type CreateRequestSchema = OpenApi.DogApi.Schemas.CreateRequest
module CreateDogRequestSchema =
    let deserializeFromBytes (bytes:byte []) = 
        try
            bytes
            |> String.fromBytes
            |> CreateRequestSchema.Parse
            |> Ok
        with 
        | ex ->
            sprintf "could not parse CreateRequestSchema\n%A" ex
            |> DogError
            |> Error
    let toDomain (schema:CreateRequestSchema) =
        result {
            let dogId = 
                schema.DogId 
                |> DogId.fromGuid
            let! dog =
                schema.Dog
                |> DogSchema.toDomain
            let envelope =
                { EffectiveDate = schema.EffectiveDate
                  Data = dog }
            return (dogId, envelope)
        }

type ReverseRequestSchema = OpenApi.DogApi.Schemas.ReverseRequest
module ReverseRequestSchema =
    let deserializeFromBytes (bytes:byte []) =
        try
            bytes
            |> String.fromBytes
            |> ReverseRequestSchema.Parse
            |> Ok
        with 
        | ex ->
            sprintf "could not parse ReverseRequestSchema %A" ex
            |> DogError
            |> Error
    let toDomain (schema:ReverseRequestSchema) =
        let dogId = 
            schema.DogId 
            |> DogId.fromGuid
        (dogId, schema.EventNumber)

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
