module Dog.Ouroboros

open Equinox
open System
open SimpleType

type Entity = private Entity of String50
module Entity =
    let value (Entity entity) = entity |> String50.value
    let create entity = entity |> String50.create |> Result.map Entity

type ObservationDate =
    | Latest
    | AsOf of DateTime
    | AsAt of DateTime

type Event<'DomainEvent> =
    { CreatedDate: DateTime
      EffectiveDate: DateTime
      EffectiveOrder: int
      DomainEvent: 'DomainEvent }

type Command<'DomainCommand> =
    { EffectiveDate: DateTime
      DomainCommand: 'DomainCommand }

type Evolve<'DomainState,'DomainEvent> = 
    'DomainState 
     -> Event<'DomainEvent>
     -> 'DomainState
type Interpret<'DomainCommand,'DomainState,'DomainEvent> = 
    Command<'DomainCommand> 
     -> 'DomainState 
     -> Event<'DomainEvent> list
type Fold<'DomainState,'DomainEvent> = 
    'DomainState 
     -> seq<Event<'DomainEvent>> 
     -> 'DomainState
type IsOrigin<'DomainEvent> = 
    Event<'DomainEvent> 
     -> bool
type Compact<'DomainState,'DomainEvent> = 
    'DomainState 
     -> Event<'DomainEvent>
type Stream<'DomainEvent,'DomainState> = 
    Store.IStream<Event<'DomainEvent>,'DomainState>
type Resolve<'DomainEvent,'DomainState> = 
    Target 
     -> Stream<'DomainEvent,'DomainState>
type Codec<'DomainEvent> = 
    Equinox.UnionCodec.IUnionEncoder<Event<'DomainEvent>,byte[]>
type Execute<'DomainCommand,'DomainError> = 
    Command<'DomainCommand> 
     -> AsyncResult<unit,'DomainError>
type Query<'DomainState,'DomainError> = 
    ObservationDate 
     -> AsyncResult<'DomainState,'DomainError>

type Aggregate<'State,'Command,'Event> =
    { entity: Entity
      intial: 'State
      codec: Codec<'Event>
      isOrigin: IsOrigin<'Event>
      compact: Compact<'State,'Event>
      fold: Fold<'State,'Event>
      evolve: Evolve<'State,'Event>
      interpret: Interpret<'Command,'State,'Event> }

type Repository<'DomainCommand,'DomainState,'DomainError> =
    { execute: Execute<'DomainCommand,'DomainError>
      query: Query<'DomainState,'DomainError> }
