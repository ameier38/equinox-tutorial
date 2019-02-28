namespace Lease

open FSharp.UMX
open SimpleType
open System

type Entity = private Entity of String50
module Entity =
    let value (Entity entity) = entity |> String50.value
    let create entity = entity |> String50.create |> Result.map Entity

type [<Measure>] eventId
type EventId = int<eventId>

type [<Measure>] createdDate
type CreatedDate = DateTime<createdDate>

type [<Measure>] effectiveDate
type EffectiveDate = DateTime<effectiveDate>

type ObservationDate =
    | Latest
    | AsOf of DateTime
    | AsAt of DateTime

type Context =
    { EventId: EventId
      CreatedDate: CreatedDate
      EffectiveDate: EffectiveDate }
module Context =
    let create eventId effectiveDate =
        { EventId = eventId
          CreatedDate = %DateTime.UtcNow
          EffectiveDate = effectiveDate }

type Apply<'DomainEvent,'DomainState> =
    'DomainState
     -> 'DomainEvent
     -> 'DomainState

type Decide<'DomainCommand,'DomainEvent,'DomainState> =
    EventId
     -> 'DomainCommand
     -> 'DomainState
     -> Result<'DomainEvent list,string>

type StreamState<'DomainEvent> = 
    { NextId: EventId 
      Events: 'DomainEvent list }

type Reconstitute<'DomainEvent,'DomainState> =
    ObservationDate
     -> 'DomainEvent list
     -> 'DomainState

type Evolve<'DomainEvent> = 
    StreamState<'DomainEvent>
     -> 'DomainEvent
     -> StreamState<'DomainEvent>

type Interpret<'DomainCommand,'DomainEvent> = 
    'DomainCommand 
     -> StreamState<'DomainEvent>
     -> Result<unit,string> * 'DomainEvent list

type IsOrigin<'DomainEvent> = 
    'DomainEvent
     -> bool

type Compact<'DomainEvent> = 
    StreamState<'DomainEvent>
     -> 'DomainEvent

type Execute<'EntityId,'DomainCommand> =
    'EntityId
     -> 'DomainCommand 
     -> AsyncResult<unit,string>

type Projection<'DomainEvent,'View> =
    ObservationDate
     -> StreamState<'DomainEvent>
     -> 'View

type Query<'EntityId,'DomainEvent,'View> = 
    'EntityId
     -> ObservationDate 
     -> Projection<'DomainEvent,'View>
     -> AsyncResult<'View,string>

type Aggregate<'DomainCommand,'DomainEvent,'DomainState> =
    { entity: string
      initial: 'DomainState
      isOrigin: IsOrigin<'DomainEvent>
      apply: Apply<'DomainEvent,'DomainState>
      decide: Decide<'DomainCommand,'DomainEvent,'DomainState>
      reconstitute: Reconstitute<'DomainEvent,'DomainState>
      compact: Compact<'DomainEvent>
      evolve: Evolve<'DomainEvent>
      interpret: Interpret<'DomainCommand,'DomainEvent> }

type Handler<'EntityId,'DomainCommand,'DomainEvent,'DomainState,'View> =
    { execute: Execute<'EntityId,'DomainCommand>
      query: Query<'EntityId,'DomainEvent,'View> }
