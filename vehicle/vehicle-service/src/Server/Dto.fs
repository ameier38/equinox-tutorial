module Server.Dto

open CosmicDealership.Vehicle
open Shared

module Vehicle =
    let fromProto (proto:V1.Vehicle) =
        let vehicleId = VehicleId.fromString proto.VehicleId
        { VehicleId = vehicleId
          Make = proto.Make
          Model = proto.Model
          Year = proto.Year }

    let toProto (vehicle:Vehicle) =
        V1.Vehicle(
            VehicleId=(VehicleId.toString vehicle.VehicleId),
            Make=vehicle.Make,
            Model=vehicle.Model,
            Year=vehicle.Year)

module VehicleUpdates =
    let fromProto (proto:V1.VehicleUpdates) =
        let make =
            match proto.MakeCase with
            | V1.VehicleUpdates.MakeOneofCase.MakeValue -> Some proto.MakeValue
            | _ -> None
        let model =
            match proto.ModelCase with
            | V1.VehicleUpdates.ModelOneofCase.ModelValue -> Some proto.ModelValue
            | _ -> None
        let year =
            match proto.YearCase with
            | V1.VehicleUpdates.YearOneofCase.YearValue -> Some proto.YearValue
            | _ -> None
        { Make = make
          Model = model
          Year = year }
