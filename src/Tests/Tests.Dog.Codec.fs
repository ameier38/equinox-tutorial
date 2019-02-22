module Tests.Dog.Codec

open Dog.Codec
open Dog.Ouroboros
open Expecto

type Dog = 
    { Name: string
      Breed: string }

type DogEvent =
    | Born of Dog
    | Ate
    | Slept
    interface TypeShape.UnionContract.IUnionContract


let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()

let mkEncoder<'Union when 'Union :> TypeShape.UnionContract.IUnionContract> =
    JsonUtf8.Create<'Union>(serializationSettings)
let encoder = lazy(mkEncoder<DogEvent>)

let ``Basic enc/dec round trip`` (event: Event<DogEvent>) =
    let e = encoder.Value
    e.TryDecode(e.Encode event) = Some event

[<Tests>]
let testCodec = testList "test Dog.Codec" [
    testProperty "rount trip" ``Basic enc/dec round trip``
]
