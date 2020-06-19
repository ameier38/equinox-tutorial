namespace Shared

open FSharp.UMX
open System
open System.IO

type [<Measure>] vehicleId
type VehicleId = Guid<vehicleId>

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

module VehicleId =
    let toString (vehicleId:VehicleId) = Guid.toStringN vehicleId
    let fromString (s:string) = Guid.parse<vehicleId> s
    let create () = Guid.NewGuid() |> UMX.tag<vehicleId>

module Env = 
    let getEnv (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrEmpty(value) -> defaultValue
        | value -> value
    let getSecret (secretsDir:string) (secretName:string) (secretKey:string) (defaultEnv:string) (defaultValue:string) =
        let secretPath = Path.Join(secretsDir.AsSpan(), secretName.AsSpan(), secretKey.AsSpan())
        if File.Exists(secretPath) then
            File.ReadAllText(secretPath).Trim()
        else
            getEnv defaultEnv defaultValue
