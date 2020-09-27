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

type VehicleClient(vehicleProcessorConfig:VehicleProcessorConfig, mongoConfig:MongoConfig) =
    let vehicleChannel = Channel(vehicleProcessorConfig.Url, ChannelCredentials.Insecure)
    let vehicleService = CosmicDealership.Vehicle.V1.VehicleService.VehicleServiceClient(vehicleChannel)
    do Log.Information("🔗 Connected to Vehicle API at {Url}", vehicleProcessorConfig.Url)
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
                GetVehicleResponse.VehicleNotFound { message = sprintf "could not find Vehicle-%s" input.vehicleId }
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
            GetAvailableVehicleResponse.VehicleNotFound { message = sprintf "could not find Vehicle-%s" input.vehicleId }
        else
            GetAvailableVehicleResponse.Vehicle vehicle

    member _.AddVehicle(user:User, input:VehicleInput) =
        try
            let userProto = User.toProto user
            let vehicleProto =
                CosmicDealership.Vehicle.V1.Vehicle(
                    Make = input.make,
                    Model = input.model,
                    Year = input.year)
            let req =
                CosmicDealership.Vehicle.V1.AddVehicleRequest(
                    User = userProto,
                    VehicleId = input.vehicleId,
                    Vehicle = vehicleProto)
            Log.Debug("Adding vehicle {@Request}", req)
            let res = vehicleService.AddVehicle(req)
            Log.Debug("Successfully added vehicle {@Response}", res)
            res
        with ex ->
            Log.Error(ex, "Error adding Vehicle-{VehicleId}", input.vehicleId)
            raise ex

    member _.UpdateVehicle(user:User, input:VehicleInput) =
        try
            let userProto = User.toProto user
            let vehicle = 
                CosmicDealership.Vehicle.V1.Vehicle(
                    Make = input.make,
                    Model = input.model,
                    Year = input.year)
            let req =
                CosmicDealership.Vehicle.V1.UpdateVehicleRequest(
                    User = userProto,
                    VehicleId = input.vehicleId,
                    Vehicle = vehicle)
            Log.Debug("Updating vehicle {@Request}", req)
            let res = vehicleService.UpdateVehicle(req)
            Log.Debug("Successfully updated Vehicle-{VehicleId}", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error updating Vehicle-{VehicleId}", input.vehicleId)
            raise ex

    member _.UpdateAvatar(user:User, input:UpdateVehicleAvatarInput) =
        let userProto = User.toProto user
        let req =
            CosmicDealership.Vehicle.V1.UpdateAvatarRequest(
                User = userProto,
                VehicleId = input.vehicleId,
                AvatarUrl = input.avatarUrl)
        Log.Debug("Updating vehicle avatar {@Request}", req)
        try
            let res = vehicleService.UpdateAvatar(req)
            Log.Debug("Successfully updated Vehicle-{VehicleId} avatar", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error updating Vehicle-{VehicleId} avatar", input.vehicleId)
            raise ex

    member _.RemoveAvatar(user:User, input:RemoveVehicleAvatarInput) =
        let userProto = User.toProto user
        let req =
            CosmicDealership.Vehicle.V1.RemoveAvatarRequest(
                User = userProto,
                VehicleId = input.vehicleId)
        Log.Debug("Removing vehicle avatar {@Request}", req)
        try
            let res = vehicleService.RemoveAvatar(req)
            Log.Debug("Successfully removed Vehicle-{VehicleId} avatar", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error removing Vehicle-{VehicleId} avatar", input.vehicleId)
            raise ex

    member _.AddImage(user:User, input: AddVehicleImageInput) =
        let userProto = User.toProto user
        let req =
            CosmicDealership.Vehicle.V1.AddImageRequest(
                User = userProto,
                VehicleId = input.vehicleId,
                ImageUrl = input.imageUrl)
        Log.Debug("Adding vehicle image {@Request}", req)
        try
            let res = vehicleService.AddImage(req)
            Log.Debug("Successfully added image to Vehicle-{VehicleId}", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error adding image for Vehicle-{VehicleId}", input.vehicleId)
            raise ex
    
    member _.RemoveImage(user:User, input: RemoveVehicleImageInput) =
        let userProto = User.toProto user
        let req =
            CosmicDealership.Vehicle.V1.RemoveImageRequest(
                User = userProto,
                VehicleId = input.vehicleId,
                ImageUrl = input.imageUrl)
        Log.Debug("Removing vehicle image {@Request}", req)
        try
            let res = vehicleService.RemoveImage(req)
            Log.Debug("Successfully removed image from Vehicle-{VehicleId}", input.vehicleId)
            res
        with ex ->
            Log.Error(ex, "Error removing image from Vehicle-{VehicleId}", input.vehicleId)
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
            res
        with ex ->
            Log.Error("Error removing Vehicle-{VehicleId}", input.vehicleId)
            raise ex
