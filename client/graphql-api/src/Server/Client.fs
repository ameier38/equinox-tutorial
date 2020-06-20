namespace Server

open FSharp.UMX
open Grpc.Core
open MongoDB.Bson
open MongoDB.Driver
open Serilog
open System

type VehicleClient(vehicleApiConfig:VehicleApiConfig, mongoConfig:MongoConfig) =
    let vehicleChannel = Channel(vehicleApiConfig.Url, ChannelCredentials.Insecure)
    let vehicleService = Tutorial.Vehicle.V1.VehicleService.VehicleServiceClient(vehicleChannel)
    do Log.Information("🔗 Connected to Vehicle API at {Url}", vehicleApiConfig.Url)
    let store = Store(mongoConfig)
    let vehiclesCollection = store.GetCollection<VehicleStateDto>("vehicles")
    do Log.Information("🍃 Connected to MongoDB at {Url}", mongoConfig.Url)

    let authorize (user:User) (permission:string) =
        if not (user.Permissions |> Seq.contains permission) then
            failwithf "user %s does not have %s permission" %user.UserId permission

    member _.ListVehicles(user:User, input:ListVehiclesInput) =
        authorize user "list:vehicles"
        let pageToken = input.pageToken |> Option.defaultValue ""
        let pageSize = input.pageSize |> Option.defaultValue 10
        let statusFilter = Builders<VehicleStateDto>.Filter.In((fun doc -> doc.status), ["Available"; "Leased"])
        let idFilter = 
            if pageToken = "" then
                Builders.Filter.Empty
            else
                let oid = ObjectId.Parse(pageToken)
                Builders<VehicleStateDto>.Filter.Gt((fun doc -> doc._id), oid)
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
        let vehicleIdFilter = Builders<VehicleStateDto>.Filter.Where(fun doc -> doc.vehicleId = input.vehicleId)
        vehiclesCollection.Find(vehicleIdFilter).First()

    member _.AddVehicle(user:User, input:AddVehicleInput) =
        let userProto = User.toProto user
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId = (Guid.NewGuid().ToString("N")),
                Make = input.make,
                Model = input.model,
                Year = input.year)
        let req =
            Tutorial.Vehicle.V1.AddVehicleRequest(
                User = userProto,
                Vehicle = vehicle)
        Log.Debug("Adding vehicle {@Request}", req)
        let res = vehicleService.AddVehicle(req)
        Log.Debug("Successfully added vehicle")
        res.Message
