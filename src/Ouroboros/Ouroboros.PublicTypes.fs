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
          CreatedDate = % DateTime.UtcNow
          EffectiveDate = effectiveDate }

type Apply<'DomainState,'DomainEvent> =
    'DomainState
     -> 'DomainEvent
     -> 'DomainState

type Decide<'DomainCommand,'DomainState,'DomainEvent> =
    'DomainCommand
     -> 'DomainState
     -> Result<'DomainEvent list,string>

type Stream<'DomainEvent> = 
    { NextId: EventId 
      Events: 'DomainEvent list }

type Reconstitute<'DomainEvent,'DomainState> =
    ObservationDate
     -> 'DomainEvent list
     -> 'DomainState

type Evolve<'DomainEvent> = 
    Stream<'DomainEvent>
     -> 'DomainEvent
     -> Stream<'DomainEvent>

type Interpret<'DomainCommand,'DomainEvent> = 
    'DomainCommand 
     -> Stream<'DomainEvent>
     -> Result<unit,string> * 'DomainEvent list

type IsOrigin<'DomainEvent> = 
    'DomainEvent
     -> bool

type Compact<'DomainEvent> = 
    Stream<'DomainEvent>
     -> 'DomainEvent

type Execute<'EntityId,'DomainCommand> =
    'EntityId
     -> 'DomainCommand 
     -> AsyncResult<unit,string>

type Query<'EntityId,'DomainState> = 
    'EntityId
     -> ObservationDate 
     -> AsyncResult<'DomainState,string>
