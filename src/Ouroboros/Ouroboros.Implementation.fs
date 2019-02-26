namespace Ouroboros

open System
open FSharp.UMX

module EventMeta =
    let create eventId effectiveDate =
        { EventId = eventId
          CreatedDate = % DateTime.UtcNow
          EffectiveDate = effectiveDate }

type Aggregate<'DomainState,'DomainCommand,'DomainEvent> =
    { entity: Entity
      initial: 'DomainState
      isOrigin: IsOrigin<'DomainEvent>
      compact: Compact<'DomainEvent>
      evolve: Evolve<'DomainEvent>
      interpret: Interpret<'DomainCommand,'DomainEvent> }

type Handler<'DomainCommand,'DomainState,'DomainError> =
    { execute: Execute<'DomainCommand,'DomainError>
      query: Query<'DomainState,'DomainError> }
