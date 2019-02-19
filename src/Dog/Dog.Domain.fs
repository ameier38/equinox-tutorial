namespace Dog

open System

type EmptyCommandEnvelope =
    { EffectiveDate: DateTime }

type CommandEnvelope<'Data> =
    { EffectiveDate: DateTime
      Data: 'Data }

type EmptyEventEnvelope =
    { EffectiveDate: DateTime
      EffectiveOrder: int }

type EventEnvelope<'Data> =
    { EffectiveDate: DateTime
      EffectiveOrder: int
      Data: 'Data }

type DogError = DogError of string

type Dog =
    { Name: Name
      Breed: Breed
      BirthDate: DateTime }

type DogState =
    { Dog: Dog
      Age: Age
      Weight: Weight }

type State =
    | NonExistent
    | Corrupt of string
    | Bored of DogState
    | Tired of DogState
    | Hungry of DogState

type Command =
    | Reverse of int
    | Create of CommandEnvelope<Dog>
    | Play of EmptyCommandEnvelope
    | Sleep of CommandEnvelope<TimeSpan>
    | Eat of CommandEnvelope<Weight>

type Event =
    | Reversed of int
    | Born of EventEnvelope<Dog>
    | Played of EmptyEventEnvelope
    | Slept of EventEnvelope<TimeSpan>
    | Ate of EventEnvelope<Weight>
    interface TypeShape.UnionContract.IUnionContract
