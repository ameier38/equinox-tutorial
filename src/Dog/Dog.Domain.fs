namespace Dog

open System
open Ouroboros

type DogError = DogError of string

type Dog =
    { Name: Name
      Breed: Breed
      BirthDate: DateTime }

type DogStateData =
    { Dog: Dog
      Age: Age
      Weight: Weight }

type DogState =
    | NonExistent
    | Corrupt of DogError
    | Bored of DogStateData
    | Tired of DogStateData
    | Hungry of DogStateData

type DogCommand =
    | Undo of EventId
    | Create of Dog
    | Modify of Dog
    | Play of TimeSpan
    | Sleep of TimeSpan
    | Eat of Weight

type DogEvent =
    | Undid of EventId
    | Born of Dog
    | Modified of Dog
    | Played of TimeSpan
    | Slept of TimeSpan
    | Ate of Weight
    interface TypeShape.UnionContract.IUnionContract
