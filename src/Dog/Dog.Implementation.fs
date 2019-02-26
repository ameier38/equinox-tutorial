module Dog.Implementation

open Ouroboros
open Equinox.EventStore
open Serilog
open System

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
        | Undid _ -> 0
        | Born _ -> 1
        | Played _ -> 2
        | Slept _ -> 3
        | Ate _ -> 4

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

module Aggregate =
    let (|DomainEvent|) (event:Event<DogEvent>) = event.DomainEvent
    let (|EventId|) (event:Event<DogEvent>) = event.EventId

    let evolve : Evolve<DogEvent> =
        fun state ((DomainEvent domainEvent) as event) ->
            match domainEvent with
            | Undid undoEventId -> state |> List.filter (fun (EventId eventId) -> eventId <> undoEventId )
            | _ -> event :: state

    let apply 
        (effectiveDate:EffectiveDate) 
        : Apply<DogState,DogEvent> =
        fun state -> function
            | Played timeSpan ->
                match state with
                | Corrupt err -> Corrupt err
                | Bored dogState ->
                    dogState
                    |> DogState.updateWeight (Weight -1m<g>)
                    |> DogState.updateAge effectiveDate
                    |> Result.mapError DogError
                    |> Result.bimap Tired Corrupt
                | _ ->
                    sprintf "dog could not have played in state %A" state
                    |> DogError
                    |> Corrupt
            | Slept { EffectiveDate = effectiveDate } ->
                match state with
                | Corrupt err -> Corrupt err
                | Tired dogState ->
                    dogState
                    |> DogState.updateAge effectiveDate
                    |> Result.mapError DogError
                    |> Result.bimap Hungry Corrupt
                | _ ->
                    sprintf "dog could not have slept in state %A" state
                    |> DogError
                    |> Corrupt
            | Ate { EffectiveDate = effectiveDate; Data = weight } ->
                match state with
                | Corrupt err -> Corrupt err
                | Hungry dogState ->
                    dogState
                    |> DogState.updateWeight weight
                    |> DogState.updateAge effectiveDate
                    |> Result.mapError DogError
                    |> Result.bimap Tired Corrupt
                | _ ->
                    sprintf "dog could not have ate in state %A" state
                    |> DogError
                    |> Corrupt

    let interpret command state =
        match command with
        | Reverse eventNumber -> [Reversed eventNumber]
        | Create { EffectiveDate = effectiveDate; Data = dog } ->
            match state with
            | NonExistent ->
                { EffectiveDate = effectiveDate
                  Data = dog }
                |> Born
                |> List.singleton
            | _ -> []
        | Play { EffectiveDate = effectiveDate } ->
            match state with
            | Bored dogState ->
                { EffectiveDate = effectiveDate }
                |> Played
                |> List.singleton
            | _ -> []
        | Sleep { EffectiveDate = effectiveDate; Data = timeSpan } ->
            match state with
            | Tired dogState ->
                { EffectiveDate = effectiveDate
                  Data = timeSpan }
                |> Slept
                |> List.singleton
            | _ -> []
        | Eat { EffectiveDate = effectiveDate; Data = weight } ->
            match state with
            | Hungry dogState ->
                { EffectiveDate = effectiveDate
                  Data = weight }
                |> Ate
                |> List.singleton
            | _ -> []
     
let aggregate =
    let isOrigin { DomainEvent = dogEvent } =
        match dogEvent with
        | 
    result {
        let! entity = "dog" |> Entity.create
        return
            { entity = entity
              initial = NonExistent
              isOrigin = }
    }
module Aggregate =
    let fold state = Seq.fold evolve state

type Handler =
    { execute: Command -> AsyncResult<State, DogError>
      query: unit -> AsyncResult<State, DogError> }
module Handler =
    let create log stream =
        let inner = Equinox.Handler(Aggregate.fold, log, stream, maxAttempts = 2)
        let execute command = 
            try
                inner.Decide(fun ctx -> 
                    ctx.Execute (interpret command)
                    ctx.State)
                |> AsyncResult.ofAsync
            with 
            | exn ->
                sprintf "execute failed: \n%A" exn
                |> DogError
                |> AsyncResult.ofError
        let query () = 
            try
                inner.Query(id)
                |> AsyncResult.ofAsync
            with
            | exn ->
                sprintf "query failed: \n%A" exn
                |> DogError
                |> AsyncResult.ofError
        { execute = execute
          query = query }

type Service =
    { execute: DogId -> Command -> AsyncResult<State, DogError>
      get: DogId -> AsyncResult<State, DogError> }
module Service =
    let create log resolveStream maxAttempts =
        let (|AggregateId|) (dogId: DogId) = Equinox.AggregateId("Dog", DogId.toString dogId)
        let (|Stream|) (AggregateId dogId) = Equinox.Stream(log, resolveStream dogId, defaultArg maxAttempts 3)
        let handle (Stream stream) command =
            try
                stream.Transact(fun state ->
                    let ctx = Equinox.Accumulator(Aggregate.fold, state)
                    ctx.Execute(interpret command)
                    (ctx.State, ctx.Accumulated))
                |> AsyncResult.ofAsync
            with
            | exn ->
                sprintf "error handling %A:\n%A" command exn
                |> DogError
                |> AsyncResult.ofError
        
        let read (AggregateId dogId) observationDate = 
            let fold state (events:seq<Event>) = 
                events 
                |> Seq.filter (function
                    | Born )
            let ctx = Equinox.Accumulator
            stream.Load
        let get (Stream stream) =
            stream
        { execute = execute
          get = get }

module Log =
    let log = 
        Log.Logger
        |> Logger.SerilogNormal

module Store =
    let connect (config:EventStoreConfig) (name:string) =
        let uri = 
            sprintf "%s://@%s:%d" 
                config.Protocol 
                config.Host 
                config.Port
            |> Uri
        let timeout = TimeSpan.FromSeconds 5.0
        let connector = 
            GesConnector(
                config.User, 
                config.Password, 
                reqTimeout=timeout, 
                reqRetries=1, 
                log=Log.log)
        let cache = Caching.Cache ("ES", 20)
        let strategy = ConnectionStrategy.ClusterTwinPreferSlaveReads
        let conn = 
            connector.Establish(name, Discovery.Uri uri, strategy)
            |> Async.RunSynchronously
        let gateway = GesGateway(conn, GesBatchingPolicy(maxBatchSize=500))
        (gateway, cache)

module Repository =
    let create (store:Store) (aggregate:Aggregate<) =
        
