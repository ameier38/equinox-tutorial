module Dog.Implementation

open System
open Serilog

module Dog =
    let create name breed birthDate : Result<Dog,DogError> =
        result {
            let! name' = 
                name 
                |> Name.create 
                |> Result.mapError DogError
            let! breed' = 
                breed 
                |> Breed.create 
                |> Result.mapError DogError
            return
                { Name = name'
                  Breed = breed'
                  BirthDate = birthDate }
        }

module Event =
    let getEffectiveOrder = function
        | Reversed _ -> 0
        | Born _ -> 1
        | Played _ -> 2
        | Slept _ -> 3
        | Ate _ -> 4

let initial = NonExistent

let calculateAge (birthDate:DateTime) (effectiveDate:DateTime) =
    effectiveDate.Year - birthDate.Year
    |> Years.fromInt
    |> Age.create

module DogState =
    let updateAge effectiveDate dogState =
        let { Dog = { BirthDate = birthDate}} = dogState
        result {
            let! age = 
                calculateAge birthDate effectiveDate
            return
                { dogState with
                    Age = age }
        }
    let updateWeight difference dogState =
        { dogState with Weight = dogState.Weight |> Weight.add difference }

let evolve state = function
    | Born { Data = dog } ->
        match state with
        | Corrupt err -> Corrupt err
        | NonExistent -> 
            result {
                let age = Age.zero
                let weight = 300m<g> |> Weight.create
                return
                    { Dog = dog
                      Age = age
                      Weight = weight }
            } |> Result.bimap Bored Corrupt
        | _ -> "dog could not have been born; dog already exists" |> Corrupt
    | Played { EffectiveDate = effectiveDate } ->
        match state with
        | Corrupted err -> Corrupted err
        | Bored dogState ->
            dogState
            |> DogState.updateWeight (Weight -1m<g>)
            |> DogState.updateAge effectiveDate
            |> Result.bimap Tired Corrupted
        | _ ->
            sprintf "dog could not have played in state %A" state
            |> DogError
            |> Corrupted
    | Slept { EffectiveDate = effectiveDate } ->
        match state with
        | Corrupted err -> Corrupted err
        | Tired dogState ->
            dogState
            |> DogState.updateAge effectiveDate
            |> Result.bimap Hungry Corrupted
        | _ ->
            sprintf "dog could not have slept in state %A" state
            |> DogError
            |> Corrupted
    | Ate { EffectiveDate = effectiveDate; Data = weight } ->
        match state with
        | Corrupted err -> Corrupted err
        | Hungry dogState ->
            dogState
            |> DogState.updateWeight weight
            |> DogState.updateAge effectiveDate
            |> Result.bimap Tired Corrupted
        | _ ->
            sprintf "dog could not have ate in state %A" state
            |> DogError
            |> Corrupted

let interpret command state =
    match command with
    | Create { EffectiveDate = effectiveDate; Data = dog } ->
        match state with
        | NonExistent ->
            { EffectiveDate = effectiveDate
              EffectiveOrder = 0
              Data = dog }
            |> Born
            |> List.singleton
        | _ -> []
    | Play { EffectiveDate = effectiveDate } ->
        match state with
        | Bored dogState ->
            { EffectiveDate = effectiveDate
              EffectiveOrder = 1 }
            |> Played
            |> List.singleton
        | _ -> []
    | Sleep { EffectiveDate = effectiveDate; Data = timeSpan } ->
        match state with
        | Tired dogState ->
            { EffectiveDate = effectiveDate
              EffectiveOrder = 1
              Data = timeSpan }
            |> Slept
            |> List.singleton
        | _ -> []
    | Eat { EffectiveDate = effectiveDate; Data = weight } ->
        match state with
        | Hungry dogState ->
            { EffectiveDate = effectiveDate
              EffectiveOrder = 1
              Data = weight }
            |> Ate
            |> List.singleton
        | _ -> []
     
let fold state = Seq.fold evolve state

let isOrigin = function NonExistent -> true | _ -> false

type Handler =
    { execute: Command -> Async<State>
      query: unit -> Async<string> }
module Handler =
    let create log stream =
        let inner = Equinox.Handler(fold, log, stream, maxAttempts = 2)
        let execute command = 
            inner.Decide(fun ctx -> 
                ctx.Execute (interpret command)
                ctx.State)
        let query () = 
            inner.Query(fun state -> state.ToString())
        { execute = execute
          query = query }

type Service =
    { execute: DogId -> Command -> Async<State> }
module Service =
    let create log resolve =
        let (|AggregateId|) (dogId: DogId) = Equinox.AggregateId("Dog", DogId.toStringN dogId)
        let (|DogHandler|) (AggregateId dogId) = Handler.create log (resolve dogId)
        let execute (DogHandler handler) command = handler.execute command
        { execute = execute }
