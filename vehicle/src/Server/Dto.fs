module Server.Dto

open CosmicDealership.Vehicle
open FSharp.ValidationBlocks
open Result.Builders
open Shared

module Vehicle =
    let fromProto (proto:V1.Vehicle) =
        result {
            let! make = Block.validate<Make> proto.Make |> Result.mapError (fun e -> e.ToString())
            let! model = Block.validate<Model> proto.Model |> Result.mapError (fun e -> e.ToString())
            let! year = Block.validate<Year> proto.Year |> Result.mapError (fun e -> e.ToString())
            return 
                { Make = make
                  Model = model
                  Year = year }
        }

    let toProto (vehicle:Vehicle) =
        V1.Vehicle(
            Make=(vehicle.Make |> Block.value),
            Model=(vehicle.Model |> Block.value),
            Year=(vehicle.Year |> Block.value))
