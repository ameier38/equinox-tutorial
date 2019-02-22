namespace Dog

open System

type ObservationDate =
    | Latest
    | AsOf of DateTime
    | AsAt of DateTime

type EmptyEnvelope =
    { EffectiveDate: DateTime }

type Envelope<'Data> =
    { EffectiveDate: DateTime
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
    | Corrupt of DogError
    | Bored of DogState
    | Tired of DogState
    | Hungry of DogState

type Command =
    | Reverse of int
    | Create of Envelope<Dog>
    | Play of EmptyEnvelope
    | Sleep of Envelope<TimeSpan>
    | Eat of Envelope<Weight>

type Event =
    | Reversed of int
    | Born of Envelope<Dog>
    | Played of EmptyEnvelope
    | Slept of Envelope<TimeSpan>
    | Ate of Envelope<Weight>
    interface TypeShape.UnionContract.IUnionContract

