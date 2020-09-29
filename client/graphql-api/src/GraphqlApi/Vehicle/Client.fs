module GraphqlApi.Vehicle.Client

open GraphqlApi
open GraphqlApi.Vehicle.Types
open Serilog

type VehicleClient
    (vehicleCommandService:CosmicDealership.Vehicle.V1.VehicleCommandService.VehicleCommandServiceClient,
     vehicleQueryService:CosmicDealership.Vehicle.V1.VehicleQueryService.VehicleQueryServiceClient) =

    member _.ListAvailableVehicles(input:ListVehiclesInput) =
        try
            let req =
                CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest(
                    PageToken=(input.pageToken |> Option.defaultValue null),
                    PageSize=(input.pageSize |> Option.defaultValue 10))
            vehicleQueryService.ListAvailableVehicles(req)
        with ex ->
            Log.Error(ex, "Error listing available vehicles")
            raise ex

    member _.ListVehicles(user:User, input:ListVehiclesInput) =
        try
            let req =
                CosmicDealership.Vehicle.V1.ListVehiclesRequest(
                    User=(User.toProto user),
                    PageToken=(input.pageToken |> Option.defaultValue null),
                    PageSize=(input.pageSize |> Option.defaultValue 10))
            vehicleQueryService.ListVehicles(req)
        with ex ->
            Log.Error(ex, "Error listing vehicles")
            raise ex

    member _.GetAvailableVehicle(input:GetVehicleInput) =
        try
            let req =
                CosmicDealership.Vehicle.V1.GetAvailableVehicleRequest(
                    VehicleId=input.vehicleId)
            vehicleQueryService.GetAvailableVehicle(req)
        with ex ->
            Log.Error(ex, "Error getting available Vehicle-{VehicleId}", input.vehicleId)
            raise ex

    member _.GetVehicle(user:User, input:GetVehicleInput) =
        try
            let req =
                CosmicDealership.Vehicle.V1.GetVehicleRequest(
                    User=(User.toProto user),
                    VehicleId=input.vehicleId)
            vehicleQueryService.GetVehicle(req)
        with ex ->
            Log.Error(ex, "Error getting Vehicle-{VehicleId}", input.vehicleId)
            raise ex

    member _.AddVehicle(user:User, input:VehicleInput) =
        try
            let req =
                CosmicDealership.Vehicle.V1.AddVehicleRequest(
                    User=(User.toProto user),
                    VehicleId=input.vehicleId,
                    Vehicle=CosmicDealership.Vehicle.V1.Vehicle(
                        Make = input.make,
                        Model = input.model,
                        Year = input.year))
            Log.Debug("Adding vehicle {@Request}", req)
            let res = vehicleCommandService.AddVehicle(req)
            Log.Debug("Successfully added vehicle {@Response}", res)
            res
        with ex ->
            Log.Error(ex, "Error adding Vehicle-{VehicleId}", input.vehicleId)
            raise ex

    member _.UpdateVehicle(user:User, input:VehicleInput) =
        try
            let req =
                CosmicDealership.Vehicle.V1.UpdateVehicleRequest(
                    User = (User.toProto user),
                    VehicleId = input.vehicleId,
                    Vehicle = CosmicDealership.Vehicle.V1.Vehicle(
                        Make = input.make,
                        Model = input.model,
                        Year = input.year))
            Log.Debug("Updating vehicle {@Request}", req)
            let res = vehicleCommandService.UpdateVehicle(req)
            Log.Debug("Successfully updated Vehicle-{VehicleId}", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error updating Vehicle-{VehicleId}", input.vehicleId)
            raise ex

    member _.UpdateAvatar(user:User, input:UpdateVehicleAvatarInput) =
        let req =
            CosmicDealership.Vehicle.V1.UpdateAvatarRequest(
                User = (User.toProto user),
                VehicleId = input.vehicleId,
                AvatarUrl = input.avatarUrl)
        Log.Debug("Updating vehicle avatar {@Request}", req)
        try
            let res = vehicleCommandService.UpdateAvatar(req)
            Log.Debug("Successfully updated Vehicle-{VehicleId} avatar", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error updating Vehicle-{VehicleId} avatar", input.vehicleId)
            raise ex

    member _.RemoveAvatar(user:User, input:RemoveVehicleAvatarInput) =
        let req =
            CosmicDealership.Vehicle.V1.RemoveAvatarRequest(
                User = (User.toProto user),
                VehicleId = input.vehicleId)
        Log.Debug("Removing vehicle avatar {@Request}", req)
        try
            let res = vehicleCommandService.RemoveAvatar(req)
            Log.Debug("Successfully removed Vehicle-{VehicleId} avatar", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error removing Vehicle-{VehicleId} avatar", input.vehicleId)
            raise ex

    member _.AddImage(user:User, input: AddVehicleImageInput) =
        let req =
            CosmicDealership.Vehicle.V1.AddImageRequest(
                User = (User.toProto user),
                VehicleId = input.vehicleId,
                ImageUrl = input.imageUrl)
        Log.Debug("Adding vehicle image {@Request}", req)
        try
            let res = vehicleCommandService.AddImage(req)
            Log.Debug("Successfully added image to Vehicle-{VehicleId}", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error adding image for Vehicle-{VehicleId}", input.vehicleId)
            raise ex
    
    member _.RemoveImage(user:User, input: RemoveVehicleImageInput) =
        let req =
            CosmicDealership.Vehicle.V1.RemoveImageRequest(
                User = (User.toProto user),
                VehicleId = input.vehicleId,
                ImageUrl = input.imageUrl)
        Log.Debug("Removing vehicle image {@Request}", req)
        try
            let res = vehicleCommandService.RemoveImage(req)
            Log.Debug("Successfully removed image from Vehicle-{VehicleId}", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error removing image from Vehicle-{VehicleId}", input.vehicleId)
            raise ex
    
    member _.RemoveVehicle(user:User, input:RemoveVehicleInput) =
        let req =
            CosmicDealership.Vehicle.V1.RemoveVehicleRequest(
                User = (User.toProto user),
                VehicleId = input.vehicleId)
        Log.Debug("Removing vehicle {@Request}", req)
        try
            let res = vehicleCommandService.RemoveVehicle(req)
            Log.Debug("Successfully removed Vehicle-{VehicleId}", input.vehicleId)
            res
        with ex ->
            Log.Error("Error removing Vehicle-{VehicleId}", input.vehicleId)
            raise ex
