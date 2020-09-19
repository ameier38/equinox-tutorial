module Server.Dto

open CosmicDealership.Vehicle
open FSharp.UMX
open Result.Builders
open Shared
open System.Text.RegularExpressions

module Validation =
    let validateText (s:string) =
        if Regex("[a-zA-Z0-9]+").IsMatch(s) then Ok s
        else sprintf "contains invalid characters: %s" s |> Error

module Make =
    let validate (s:string) =
        match Validation.validateText s with
        | Ok v -> v |> UMX.tag<make> |> Ok
        | Error e -> Error e

module Model =
    let validate (s:string) =
        match Validation.validateText s with
        | Ok v -> v |> UMX.tag<model> |> Ok
        | Error e -> Error e

module Year =
    let validate (i:int) =
        if i >= 1970 && i <= 3000 then i |> UMX.tag<year> |> Ok
        else sprintf "%i is not between 1970 and 3000" i |> Error

module Url =
    let validate (s:string) =
        try System.Uri(s).AbsoluteUri |> UMX.tag<url> |> Ok
        with ex -> sprintf "invalid url: %A" ex |> Error

module Vehicle =
    let fromProto (proto:V1.Vehicle) =
        result {
            let! make = Make.validate proto.Make
            let! model = Model.validate proto.Model
            let! year = Year.validate proto.Year
            return 
                { Make = make
                  Model = model
                  Year = year }
        }

    let toProto (vehicle:Vehicle) =
        V1.Vehicle(
            Make=(vehicle.Make |> UMX.untag),
            Model=(vehicle.Model |> UMX.untag),
            Year=(vehicle.Year |> UMX.untag))
