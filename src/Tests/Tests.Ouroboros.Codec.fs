module Tests.Dog.Codec

open Ouroboros
open Expecto
open FsCheck
open System

type Dog = 
    { Name: string
      Breed: string }

type DogEvent =
    | Born of Dog
    | Ate
    | Slept
    interface TypeShape.UnionContract.IUnionContract

type public EventIdGenerator() =
    static member EventId() : Arbitrary<EventId> =
        Guid.NewGuid() 
        |> EventId
        |> Gen.constant
        |> Arb.fromGen 

let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()

let mkEncoder<'Union when 'Union :> TypeShape.UnionContract.IUnionContract> =
    JsonUtf8.Create<'Union>(serializationSettings)
let encoder = lazy(mkEncoder<DogEvent>)

let ``Basic enc/dec round trip`` (event: Event<DogEvent>) =
    let e = encoder.Value
    e.TryDecode(e.Encode event) = Some event

let config = { FsCheckConfig.defaultConfig with arbitrary = [typeof<EventIdGenerator>] }

[<Tests>]
let testCodec = testList "test Dog.Codec" [
    testPropertyWithConfig config "round trip" ``Basic enc/dec round trip``
]
