namespace Server

open Grpc.Core
open Serilog
open System

type VehicleClient(config:VehicleClientConfig) =
    let vehicleChannel = Channel(config.Url, ChannelCredentials.Insecure)
    let vehicleService = Tutorial.Vehicle.V1.VehicleService.VehicleServiceClient(vehicleChannel)

    member _.ListVehicles(user:User, input:ListVehiclesInput) =
        let userProto = User.toProto user
        let req =
            Tutorial.Vehicle.V1.ListVehiclesRequest(
                User=userProto,
                PageToken=(input.PageToken |> Option.defaultValue ""),
                PageSize=(input.PageSize |> Option.defaultValue 10))
        vehicleService.ListVehicles(req)

    member _.GetVehicle(user:User, input:GetVehicleInput) =
        let userProto = User.toProto user
        let req =
            Tutorial.Vehicle.V1.GetVehicleRequest(
                User = userProto,
                VehicleId = input.VehicleId)
        let res = vehicleService.GetVehicle(req)
        res.Vehicle

    member _.AddVehicle(user:User, input:AddVehicleInput) =
        let userProto = User.toProto user
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId = (Guid.NewGuid().ToString("N")),
                Make = input.Make,
                Model = input.Model,
                Year = input.Year)
        let req =
            Tutorial.Vehicle.V1.AddVehicleRequest(
                User = userProto,
                Vehicle = vehicle)
        Log.Debug("Adding vehicle {@Request}", req)
        let res = vehicleService.AddVehicle(req)
        Log.Debug("Successfully added vehicle")
        res.Message
