namespace Ouroboros

open System
open FSharp.UMX

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

type Apply<'DomainState,'DomainEvent> =
    'DomainState
     -> 'DomainEvent
     -> 'DomainState

type Decide<'DomainCommand,'DomainState,'DomainEvent> =
    'DomainCommand
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

type Query<'EntityId,'DomainEvent,'View,'DomainState> = 
    'EntityId
     -> ObservationDate 
     -> Projection<'DomainEvent,'View>
     -> AsyncResult<'DomainState,string>

type Aggregate<'DomainState,'DomainCommand,'DomainEvent> =
    { entity: Entity
      initial: 'DomainState
      isOrigin: IsOrigin<'DomainEvent>
      apply: Apply<'DomainState,'DomainEvent>
      decide: Decide<'DomainCommand,'DomainState,'DomainEvent>
      reconstitute: Reconstitute<'DomainEvent,'DomainState>
      compact: Compact<'DomainEvent>
      evolve: Evolve<'DomainEvent>
      interpret: Interpret<'DomainCommand,'DomainEvent> }

type Handler<'EntityId,'DomainCommand,'DomainEvent,'View,'DomainState> =
    { execute: Execute<'EntityId,'DomainCommand>
      query: Query<'EntityId,'DomainEvent,'View,'DomainState> }
