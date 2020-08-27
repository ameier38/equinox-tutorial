module Server.Dto

open CosmicDealership.Vehicle
open Shared
open System

module Vehicle =
    let fromProto (proto:V1.Vehicle) =
        let vehicleId = VehicleId.fromString proto.VehicleId
        { VehicleId = vehicleId
          Make = proto.Make
          Model = proto.Model
          Year = proto.Year
          AvatarUrl = Uri(proto.AvatarUrl)
          ImageUrls = proto.ImageUrls |> Seq.map (fun url -> Uri(url)) |> Seq.toList }

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
        let avatarUrl = if isNull proto.AvatarUrl then None else Some (Uri(proto.AvatarUrl))
        { Make = make
          Model = model
          Year = year
          AvatarUrl = avatarUrl }
