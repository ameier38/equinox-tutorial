namespace Ouroboros

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

type Handler<'EntityId,'DomainCommand,'DomainState> =
    { execute: Execute<'EntityId,'DomainCommand>
      query: Query<'EntityId,'DomainState> }
