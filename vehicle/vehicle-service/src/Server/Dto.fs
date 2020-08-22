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
        let make = if isNull proto.Make then None else Some proto.Make
        let model = if isNull proto.Model then None else Some proto.Model
        let year = if proto.Year.HasValue then Some proto.Year.Value else None
        { Make = make
          Model = model
          Year = year }
