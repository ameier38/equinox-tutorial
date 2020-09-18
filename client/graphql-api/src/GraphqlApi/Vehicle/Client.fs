module GraphqlApi.Vehicle.Client

open FSharp.UMX
open GraphqlApi
open GraphqlApi.Common.Types
open GraphqlApi.Vehicle.Types
open Grpc.Core
open MongoDB.Bson
open MongoDB.Driver
open Serilog
open System

type VehicleClient(vehicleApiConfig:VehicleApiConfig, mongoConfig:MongoConfig) =
    let vehicleChannel = Channel(vehicleApiConfig.Url, ChannelCredentials.Insecure)
    let vehicleService = CosmicDealership.Vehicle.V1.VehicleService.VehicleServiceClient(vehicleChannel)
    do Log.Information("🔗 Connected to Vehicle API at {Url}", vehicleApiConfig.Url)
    let store = Store(mongoConfig)
    let vehiclesCollection = store.GetCollection<Vehicle>("vehicles")
    do Log.Information("🍃 Connected to MongoDB at {Url}", mongoConfig.Url)

    let (|Authorized|NotAuthorized|) (user:User, permission:string) =
        if user.Permissions |> Seq.contains permission then
            Authorized
        else
            let msg = sprintf "user %s does not have %s permission" %user.UserId permission
            NotAuthorized msg

    member _.ListAvailableVehicles(input:ListVehiclesInput) =
        let pageToken = input.pageToken |> Option.defaultValue ""
        let pageSize = input.pageSize |> Option.defaultValue 10
        let statusFilter = Builders<Vehicle>.Filter.Where(fun doc -> doc.status = "Available")
        let idFilter = 
            if pageToken = "" then
                Builders.Filter.Empty
            else
                let oid = ObjectId.Parse(pageToken)
                Builders<Vehicle>.Filter.Gt((fun doc -> doc._id), oid)
        let filter = Builders.Filter.And(statusFilter, idFilter)
        let vehicles = vehiclesCollection.Find(filter).Limit(new Nullable<int>(pageSize)).ToList()
        let totalCount = vehiclesCollection.CountDocuments(statusFilter)
        let nextPageToken =
            vehicles
            |> Seq.tryLast
            |> Option.map (fun v -> v._id.ToString())
            |> Option.defaultValue pageToken
        { vehicles = List.ofSeq vehicles
          totalCount = totalCount
          prevPageToken = pageToken
          nextPageToken = nextPageToken }

    member _.ListVehicles(user:User, input:ListVehiclesInput) =
        // NB: it is probably better to have a separate API for reading that manages authorization
        match user, "list:vehicles" with
        | Authorized ->
            let pageToken = input.pageToken |> Option.defaultValue ""
            let pageSize = input.pageSize |> Option.defaultValue 10
            let statusFilter = Builders<Vehicle>.Filter.In((fun doc -> doc.status), ["Available"; "Leased"])
            let idFilter = 
                if pageToken = "" then
                    Builders.Filter.Empty
                else
                    let oid = ObjectId.Parse(pageToken)
                    Builders<Vehicle>.Filter.Gt((fun doc -> doc._id), oid)
            let filter = Builders.Filter.And(statusFilter, idFilter)
            let vehicles = vehiclesCollection.Find(filter).Limit(new Nullable<int>(pageSize)).ToList()
            let totalCount = vehiclesCollection.CountDocuments(statusFilter)
            let nextPageToken =
                vehicles
                |> Seq.tryLast
                |> Option.map (fun v -> v._id.ToString())
                |> Option.defaultValue pageToken
            { vehicles = List.ofSeq vehicles
              totalCount = totalCount
              prevPageToken = pageToken
              nextPageToken = nextPageToken }
            |> ListVehiclesResponse.Vehicles
        | NotAuthorized msg ->
            ListVehiclesResponse.PermissionDenied { message = msg }

    member _.GetVehicle(user:User, input:GetVehicleInput) =
        match user, "get:vehicles" with
        | Authorized ->
            let vehicleIdFilter = Builders<Vehicle>.Filter.Where(fun doc -> doc.vehicleId = input.vehicleId)
            Log.Debug("Getting vehicle {@VehicleId}", input.vehicleId)
            let vehicle = vehiclesCollection.Find(vehicleIdFilter).FirstOrDefault()
            if isNull (box vehicle) then
                GetVehicleResponse.NotFound { message = sprintf "could not find Vehicle-%s" input.vehicleId }
            else
                GetVehicleResponse.Vehicle vehicle
        | NotAuthorized msg ->
            GetVehicleResponse.PermissionDenied { message = msg }

    member _.GetAvailableVehicle(input:GetVehicleInput) =
        let vehicleIdFilter =
            Builders<Vehicle>.Filter
                .Where(fun doc ->
                    doc.vehicleId = input.vehicleId
                    && doc.status = "Available")
        Log.Debug("Getting vehicle {@VehicleId}", input.vehicleId)
        let vehicle = vehiclesCollection.Find(vehicleIdFilter).FirstOrDefault()
        if isNull (box vehicle) then
            GetAvailableVehicleResponse.NotFound { message = sprintf "could not find Vehicle-%s" input.vehicleId }
        else
            GetAvailableVehicleResponse.Vehicle vehicle

    member _.AddVehicle(user:User, input:AddVehicleInput) =
        let vehicleId = Guid.Parse(input.vehicleId).ToString("N")
        let userProto = User.toProto user
        let vehicle =
            CosmicDealership.Vehicle.V1.Vehicle(
                VehicleId = vehicleId,
                Make = input.make,
                Model = input.model,
                Year = input.year)
        let req =
            CosmicDealership.Vehicle.V1.AddVehicleRequest(
                User = userProto,
                Vehicle = vehicle)
        Log.Debug("Adding vehicle {@Request}", req)
        try
            let res = vehicleService.AddVehicle(req)
            Log.Debug("Successfully added vehicle {@Response}", res)
            AddVehicleResponse.Success { message = res.Message }
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.PermissionDenied ->
                AddVehicleResponse.PermissionDenied { message = ex.Message }
            | StatusCode.AlreadyExists ->
                AddVehicleResponse.AlreadyExists { message = ex.Message }
            | _ ->
                Log.Error(ex, "Error adding Vehicle-{VehicleId}", vehicleId)
                raise ex
        | ex ->
            Log.Error(ex, "Error adding Vehicle-{VehicleId}", vehicleId)
            raise ex

    member _.UpdateVehicle(user:User, input:UpdateVehicleInput) =
        let userProto = User.toProto user
        let vehicleUpdates = 
            CosmicDealership.Vehicle.V1.VehicleUpdates(
                Make = (input.make |> Option.defaultValue null),
                Model = (input.model |> Option.defaultValue null),
                Year = (match input.year with Some year -> new Nullable<int>(year) | None -> new Nullable<int>()))
        let req =
            CosmicDealership.Vehicle.V1.UpdateVehicleRequest(
                User = userProto,
                VehicleId = input.vehicleId,
                VehicleUpdates = vehicleUpdates)
        Log.Debug("Updating vehicle {@Request}", req)
        try
            let res = vehicleService.UpdateVehicle(req)
            Log.Debug("Successfully updated Vehicle-{VehicleId}", input.vehicleId)
            UpdateVehicleResponse.Success { message = res.Message }
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.PermissionDenied ->
                UpdateVehicleResponse.PermissionDenied { message = ex.Message }
            | StatusCode.NotFound ->
                UpdateVehicleResponse.NotFound { message = ex.Message }
            | _ ->
                Log.Error(ex, "Error updating Vehicle-{VehicleId}", input.vehicleId)
                raise ex
        | ex ->
            Log.Error(ex, "Error updating Vehicle-{VehicleId}", input.vehicleId)
            raise ex

    member _.AddVehicleImage(user:User, input: AddVehicleImageInput) =
        let userProto = User.toProto user
        let req =
            CosmicDealership.Vehicle.V1.AddVehicleImageRequest(
                User = userProto,
                VehicleId = input.vehicleId,
                ImageUrl = input.imageUrl)
        Log.Debug("Adding vehicle image {@Request}", req)
        try
            let res = vehicleService.AddVehicleImage(req)
            Log.Debug("Successfully added image to Vehicle-{VehicleId}", input.vehicleId)
            AddVehicleImageResponse.Success { message = res.Message }
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.PermissionDenied ->
                AddVehicleImageResponse.PermissionDenied { message = ex.Message }
            | StatusCode.NotFound ->
                AddVehicleImageResponse.NotFound { message = ex.Message }
            | _ ->
                Log.Error(ex, "Error adding image for Vehicle-{VehicleId}", input.vehicleId)
                raise ex
        | ex ->
            Log.Error(ex, "Error adding image for Vehicle-{VehicleId}", input.vehicleId)
            raise ex
    
    member _.RemoveVehicle(user:User, input:RemoveVehicleInput) =
        let userProto = User.toProto user
        let req =
            CosmicDealership.Vehicle.V1.RemoveVehicleRequest(
                User = userProto,
                VehicleId = input.vehicleId)
        Log.Debug("Removing vehicle {@Request}", req)
        try
            let res = vehicleService.RemoveVehicle(req)
            Log.Debug("Successfully removed Vehicle-{VehicleId}", input.vehicleId)
            RemoveVehicleResponse.Success { message = res.Message }
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.PermissionDenied ->
                RemoveVehicleResponse.PermissionDenied { message = ex.Message }
            | StatusCode.NotFound ->
                RemoveVehicleResponse.NotFound { message = ex.Message }
            | _ ->
                Log.Error("Error removing Vehicle-{VehicleId}", input.vehicleId)
                raise ex
        | ex ->
            Log.Error("Error removing Vehicle-{VehicleId}", input.vehicleId)
            raise ex
