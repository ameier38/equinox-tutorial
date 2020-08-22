module Server.Vehicle.Client

open FSharp.UMX
open Grpc.Core
open MongoDB.Bson
open MongoDB.Driver
open Serilog
open Server
open Server.Vehicle.Types
open System

type VehicleClient(vehicleApiConfig:VehicleApiConfig, mongoConfig:MongoConfig) =
    let vehicleChannel = Channel(vehicleApiConfig.Url, ChannelCredentials.Insecure)
    let vehicleService = CosmicDealership.Vehicle.V1.VehicleService.VehicleServiceClient(vehicleChannel)
    do Log.Information("🔗 Connected to Vehicle API at {Url}", vehicleApiConfig.Url)
    let store = Store(mongoConfig)
    let vehiclesCollection = store.GetCollection<VehicleState>("vehicles")
    do Log.Information("🍃 Connected to MongoDB at {Url}", mongoConfig.Url)

    let authorize (user:User) (permission:string) =
        if not (user.Permissions |> Seq.contains permission) then
            failwithf "user %s does not have %s permission" %user.UserId permission

    member _.ListAvailableVehicles(input:ListVehiclesInput) =
        let pageToken = input.pageToken |> Option.defaultValue ""
        let pageSize = input.pageSize |> Option.defaultValue 10
        let statusFilter = Builders<VehicleState>.Filter.Where(fun doc -> doc.status = "Available")
        let idFilter = 
            if pageToken = "" then
                Builders.Filter.Empty
            else
                let oid = ObjectId.Parse(pageToken)
                Builders<VehicleState>.Filter.Gt((fun doc -> doc._id), oid)
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
        authorize user "list:vehicles"
        let pageToken = input.pageToken |> Option.defaultValue ""
        let pageSize = input.pageSize |> Option.defaultValue 10
        let statusFilter = Builders<VehicleState>.Filter.In((fun doc -> doc.status), ["Available"; "Leased"])
        let idFilter = 
            if pageToken = "" then
                Builders.Filter.Empty
            else
                let oid = ObjectId.Parse(pageToken)
                Builders<VehicleState>.Filter.Gt((fun doc -> doc._id), oid)
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

    member _.GetVehicle(user:User, input:GetVehicleInput) =
        authorize user "get:vehicles"
        let vehicleIdFilter = Builders<VehicleState>.Filter.Where(fun doc -> doc.vehicleId = input.vehicleId)
        Log.Debug("Getting vehicle {@VehicleId}", input.vehicleId)
        let vehicle = vehiclesCollection.Find(vehicleIdFilter).FirstOrDefault()
        if isNull (box vehicle) then None else Some vehicle

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
        let res = vehicleService.AddVehicle(req)
        Log.Debug("Successfully added vehicle {@Response}", res)
        res.Message

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
        let res = vehicleService.UpdateVehicle(req)
        Log.Debug("Successfully updated vehicle {@Response}", res)
        res.Message
    
    member _.RemoveVehicle(user:User, input:RemoveVehicleInput) =
        let userProto = User.toProto user
        let req =
            CosmicDealership.Vehicle.V1.RemoveVehicleRequest(
                User = userProto,
                VehicleId = input.vehicleId)
        Log.Debug("Removing vehicle {@Request}", req)
        let res = vehicleService.RemoveVehicle(req)
        Log.Debug("Successfully removed vehicle {@Response}", res)
        res.Message
