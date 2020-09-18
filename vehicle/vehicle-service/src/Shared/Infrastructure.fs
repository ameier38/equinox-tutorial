namespace Shared

open FSharp.ValidationBlocks
open System
open System.IO
open System.Text.RegularExpressions

type TextError =
    | InvalidCharacters
    | TooLong
    | Empty

type ITextBlock = inherit IBlock<string, TextError>

type Text = private Text of string with
    interface ITextBlock with
        member _.Validate =
            fun s ->
                [ if Regex("[^a-zA-Z0-9]+").IsMatch(s) then InvalidCharacters
                  if String.IsNullOrEmpty(s) then Empty ]

type Make = private Make of Text with
    interface ITextBlock with
        member _.Validate = fun _ -> []

type Model = private Model of Text with
    interface ITextBlock with
        member _.Validate = fun _ -> []

type Year = private Year of int with
    interface IBlock<int, string> with
        member _.Validate =
            fun i ->
                [ if i < 1970  then "must be greater than 1970"
                  if i > 3000 then "must be less than 3000" ]

type VehicleId = private VehicleId of Guid

module VehicleId =
    let fromString (s:string) =
        match Guid.TryParse(s) with
        | (true, vehicleId) -> VehicleId vehicleId |> Ok
        | _ -> sprintf "could not parse %s" s |> Error
    let toStringN (VehicleId vehicleId) = vehicleId.ToString("N")
    let create () = Guid.NewGuid() |> VehicleId


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
