namespace Ouroboros

open System

type ObservationDate =
    | Latest
    | AsOf of DateTime
    | AsAt of DateTime

type Context =
    { EventId: EventId
      CreatedDate: CreatedDate
      EffectiveDate: EffectiveDate }

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
      Events: 'DomainEvent list}

type Reconstitute<'DomainEvent,'DomainState> =
    ObservationDate
     -> Stream<'DomainEvent>
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

type Execute<'DomainCommand,'DomainError> = 
    'DomainCommand 
     -> AsyncResult<unit,'DomainError>

type Query<'DomainState,'DomainError> = 
    ObservationDate 
     -> AsyncResult<'DomainState,'DomainError>
