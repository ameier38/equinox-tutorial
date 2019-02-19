namespace Dog

open FSharp.UMX
open SimpleType
open System

[<Measure>] type g
module Grams =
    let fromDecimal (amt: decimal) = amt * 1m<g>
    let toDecimal (amt: decimal<g>) = amt / 1m<g>

[<Measure>] type yr
module Years =
    let fromInt (years: int) = years * 1<yr>
    let toInt (years: int<yr>) = years / 1<yr>

[<Measure>] type dogId

module Guid =
    let inline toStringN (x: Guid) = x.ToString "N"

/// Unique identifier of the dog
type DogId = Guid<dogId>
module DogId =
    let toStringN (value: DogId) = Guid.toStringN %value

/// Age of the dog
type Age = private Age of int<yr>
module Age =
    let value (Age age) = age
    let create age =
        if age >= 0<yr> then Age age |> Ok 
        else Error "age must be positive"
    let zero = Age 0<yr>

/// Name of the dog
type Name = private Name of String50
module Name =
    let value (Name name) = String50.value name
    let create name = 
        String50.create name 
        |> Result.map Name

/// Bread of the dog
type Breed = private Breed of String50
module Breed =
    let value (Breed breed) = String50.value breed
    let create breed = String50.create breed |> Result.map Breed

/// Weight in grams
type Weight = Weight of decimal<g>
module Weight =
    let value (Weight weight) = weight
    let create weight = Weight weight
    let add (Weight w1) (Weight w2) = w1 + w2 |> Weight

/// Dog's current activity
type Activity = private Activity of String50
module Activity =
    let value (Activity activity) = String50.value activity
    let create activity =
        String50.create activity
        |> Result.map Activity
