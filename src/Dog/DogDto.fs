namespace Dog

open OpenAPITypeProvider
open Ouroboros

module OpenApi =
    let [<Literal>] DogApiSchema = __SOURCE_DIRECTORY__  + "/openapi.yaml"
    type DogApi = OpenAPIV3Provider<DogApiSchema>

type DogDto =
    { Name: string
      Breed: string }
module DogDto =
    let deserializeFromBytes (bytes:byte []) = 
        try
            bytes
            |> Json.deserializeFromBytes<DogDto>
            |> Ok
        with ex ->
            sprintf "could not parse DogDto %A" ex
            |> DomainError
            |> Error
    let fromDomain (dog:Dog) =
        { Name = dog.Name |> Name.value
          Breed = dog.Breed |> Breed.value }
    let toDomain (dto:DogDto) =
        result {
            let! name = 
                dto.Name 
                |> Name.create
                |> Result.mapError DomainError
            let! breed = 
                dto.Breed 
                |> Breed.create
                |> Result.mapError DomainError
            return
                { Dog.Name = name
                  Breed = breed }
        }

type DogEventDto =
    | Reversed of int64
    | Born of DogDto
    | Ate
    | Slept
    | Woke
    | Played
module DogEventDto =
    let fromDomain = function
        | DogEvent.Reversed eventNumber ->
            eventNumber  
            |> EventNumber.value
            |> Reversed
        | DogEvent.Born dog ->
            dog
            |> DogDto.fromDomain
            |> Born
        | DogEvent.Ate -> Ate
        | DogEvent.Slept -> Slept
        | DogEvent.Woke -> Woke
        | DogEvent.Played -> Played
    let toDomain = function
        | Reversed eventNumber ->
            eventNumber
            |> EventNumber.create
            |> Result.map DogEvent.Reversed
            |> Result.mapError DomainError
        | Born dogDto ->
            dogDto
            |> DogDto.toDomain
            |> Result.map DogEvent.Born
        | Ate -> DogEvent.Ate |> Ok
        | Slept -> DogEvent.Slept |> Ok
        | Woke -> DogEvent.Woke |> Ok
        | Played -> DogEvent.Played |> Ok

type DogCommandDto =
    | Reverse of int64
    | Create of DogDto
    | Eat
    | Sleep
    | Wake
    | Play
module DogCommandDto =
    let fromDomain = function
        | DogCommand.Reverse eventNumber ->
            eventNumber
            |> EventNumber.value
            |> Reverse
        | DogCommand.Create dog -> 
            dog
            |> DogDto.fromDomain
            |> Create
        | DogCommand.Eat -> Eat
        | DogCommand.Sleep -> Sleep
        | DogCommand.Wake -> Wake
        | DogCommand.Play -> Play
    let toDomain = function
        | Reverse eventNumber ->
            eventNumber
            |> EventNumber.create
            |> Result.map DogCommand.Reverse
            |> Result.mapError DomainError
        | Create dogDto ->
            dogDto
            |> DogDto.toDomain
            |> Result.map DogCommand.Create
        | Eat -> DogCommand.Eat |> Ok
        | Sleep -> DogCommand.Sleep |> Ok
        | Wake -> DogCommand.Wake |> Ok
        | Play -> DogCommand.Play |> Ok

type DogSchema = OpenApi.DogApi.Schemas.Dog
module DogSchema =
    let toDto (schema:DogSchema) =
        { Name = schema.Name
          Breed = schema.Breed }
    let fromDto (dto:DogDto) =
        DogSchema(
            name = dto.Name,
            breed = dto.Breed)

type DogStateSchema = OpenApi.DogApi.Schemas.DogState
module DogStateSchema =
    let serializeToJson (schema:DogStateSchema) =
        schema.ToJson()
    let serializeToBytes (schema:DogStateSchema) = 
        schema
        |> serializeToJson
        |> String.toBytes

type CreateDogRequestSchema = OpenApi.DogApi.Schemas.CreateDogRequest
module CreateDogRequestSchema =
    let deserializeFromBytes (bytes:byte []) = 
        try
            bytes
            |> String.fromBytes
            |> CreateDogRequestSchema.Parse
            |> Ok
        with ex ->
            sprintf "could not parse CreateDogRequestSchema\n%A" ex
            |> DomainError
            |> Error
    let toDomain (schema:CreateDogRequestSchema) =
        result {
            let dogId = 
                schema.DogId 
                |> EntityId
            let effectiveDate = 
                schema.EffectiveDate 
                |> EffectiveDate
            let! source = 
                schema.Source 
                |> Source.create 
                |> Result.mapError DomainError
            let commandMeta =
                { EffectiveDate = effectiveDate
                  Source = source }
            let! dogCommand =
                schema.Dog
                |> DogSchema.toDto
                |> DogCommandDto.Create
                |> DogCommandDto.toDomain
            let command =
                { Data = dogCommand
                  Meta = commandMeta }
            printfn "Create:\n%A" command
            return dogId, command
        }

type ReverseRequestSchema = OpenApi.DogApi.Schemas.ReverseRequest
module ReverseRequestSchema =
    let deserializeFromBytes (bytes:byte []) =
        try
            bytes
            |> String.fromBytes
            |> ReverseRequestSchema.Parse
            |> Ok
        with ex ->
            sprintf "could not parse ReverseRequestSchema %A" ex
            |> DomainError
            |> Error
    let toDomain (schema:ReverseRequestSchema) =
        result {
            let dogId = 
                schema.DogId 
                |> EntityId
            let effectiveDate = 
                schema.EffectiveDate 
                |> EffectiveDate
            let! source = 
                schema.Source 
                |> Source.create 
                |> Result.mapError DomainError
            let commandMeta =
                { EffectiveDate = effectiveDate
                  Source = source }
            let! dogCommand =
                schema.EventNumber 
                |> int64
                |> DogCommandDto.Reverse
                |> DogCommandDto.toDomain
            let command =
                { Data = dogCommand
                  Meta = commandMeta }
            return dogId, command
        }

type CommandRequestSchema = OpenApi.DogApi.Schemas.CommandRequest
module CommandRequestSchema =
    let deserializeFromBytes (bytes:byte []) = 
        try
            bytes
            |> String.fromBytes
            |> CommandRequestSchema.Parse
            |> Ok
        with ex ->
            sprintf "could not parse CommandRequestSchema %A" ex
            |> DomainError
            |> Error
    let toDomain 
        (commandDto:DogCommandDto) =
        fun (schema:CommandRequestSchema) ->
            result {
                let dogId = 
                    schema.DogId 
                    |> EntityId
                let effectiveDate = 
                    schema.EffectiveDate 
                    |> EffectiveDate
                let! source = 
                    schema.Source 
                    |> Source.create 
                    |> Result.mapError DomainError
                let commandMeta =
                    { CommandMeta.EffectiveDate = effectiveDate
                      Source = source }
                let! dogCommand = 
                    commandDto 
                    |> DogCommandDto.toDomain
                let command =
                    { Command.Data = dogCommand
                      Meta = commandMeta }
                return dogId, command
            }

type GetDogRequestSchema = OpenApi.DogApi.Schemas.GetDogRequest
module GetDogRequestSchema =
    let deserializeFromBytes (bytes:byte []) = 
        try
            bytes
            |> String.fromBytes
            |> GetDogRequestSchema.Parse
            |> Ok
        with ex ->
            sprintf "could not parse GetRequestSchema %A" ex
            |> DomainError
            |> Error
    let (|Of|At|Invalid|) obsType =
        match obsType with
        | s when (s |> String.lower) = "of" -> Of
        | s when (s |> String.lower) = "at" -> At
        | _ -> Invalid
    let toDomain (schema:GetDogRequestSchema) =
        let dogId = schema.DogId |> EntityId
        match schema.ObservationType with 
        | Of -> 
            schema.ObservationDate 
            |> AsOf 
            |> fun obsDate -> (dogId, obsDate)
            |> Ok
        | At ->
            schema.ObservationDate 
            |> AsAt 
            |> fun obsDate -> (dogId, obsDate)
            |> Ok
        | Invalid as obsType ->
            sprintf "%s is not a valid observation type; options are 'as' or 'of'" obsType
            |> DomainError
            |> Error

type CommandResponseSchema = OpenApi.DogApi.Schemas.CommandResponse
module CommandResponseSchema =
    let serializeToJson (schema:CommandResponseSchema) =
        schema.ToJson()
    let serializeToBytes (schema:CommandResponseSchema) =
        schema
        |> serializeToJson
        |> String.toBytes
    let fromEvents (events:Event<DogEvent> list) =
        events
        |> List.map (fun ({Event.Type = eventType}) -> eventType |> EventType.value)
        |> fun eventTypes ->
            CommandResponseSchema(
                committedEvents = eventTypes)