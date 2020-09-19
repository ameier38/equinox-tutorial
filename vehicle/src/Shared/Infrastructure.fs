namespace Shared

open FSharp.UMX
open System
open System.IO

module Guid =
    let inline toStringN (x: Guid<'t>) =
        let x = x |> UMX.untag
        x.ToString("N")
    let parse<[<Measure>] 't>(s:string): Guid<'t> =
        match Guid.TryParse(s) with
        | (true, d) ->
            UMX.tag<'t> d
        | _ -> 
            failwithf "could not parse %s as Guid" s

type VehicleId = Guid<vehicleId>
and [<Measure>] vehicleId

module VehicleId =
    let toStringN (vehicleId:VehicleId) = Guid.toStringN vehicleId
    let parse (s:string) = Guid.parse<vehicleId> s
    let create () = Guid.NewGuid() |> UMX.tag<vehicleId>

type Make = string<make>
and [<Measure>] make

type Model = string<model>
and [<Measure>] model

type Year = int<year>
and [<Measure>] year

type Url = string<url>
and [<Measure>] url

module Url =
    let empty = UMX.tag<url> ""

module Env = 
    let getEnv (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrEmpty(value) -> defaultValue
        | value -> value
    let getSecret (secretsDir:string) (secretName:string) (secretKey:string) (defaultEnv:string) (defaultValue:string) =
        let secretPath = Path.Combine(secretsDir, secretName, secretKey)
        if File.Exists(secretPath) then
            File.ReadAllText(secretPath).Trim()
        else
            getEnv defaultEnv defaultValue
