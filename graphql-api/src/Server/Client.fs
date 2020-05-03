namespace Server

open Grpc.Core
open Serilog
open System

type VehicleClient(config:VehicleClientConfig) =
    let vehicleChannel = Channel(config.Url, ChannelCredentials.Insecure)
    let vehicleService = Tutorial.Vehicle.V1.VehicleService.VehicleServiceClient(vehicleChannel)

    member _.ListVehicles(input:ListVehiclesInput) =
        let req =
            Tutorial.Vehicle.V1.ListVehiclesRequest(
                PageToken=(input.PageToken |> Option.defaultValue ""),
                PageSize=(input.PageSize |> Option.defaultValue 10))
        vehicleService.ListVehicles(req)

    member _.GetVehicle(input:GetVehicleInput) =
        let req =
            Tutorial.Vehicle.V1.GetVehicleRequest(
                VehicleId = input.VehicleId)
        let res = vehicleService.GetVehicle(req)
        res.Vehicle

    member _.AddVehicle(input:AddVehicleInput) =
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId = (Guid.NewGuid().ToString("N")),
                Make = input.Make,
                Model = input.Model,
                Year = input.Year)
        let req =
            Tutorial.Vehicle.V1.AddVehicleRequest(
                Vehicle = vehicle)
        Log.Debug("Adding vehicle {@Request}", req)
        let res = vehicleService.AddVehicle(req)
        Log.Debug("Successfully added vehicle")
        res.Message
