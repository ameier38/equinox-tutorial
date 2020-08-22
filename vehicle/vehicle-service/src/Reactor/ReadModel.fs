module Reactor.ReadModel

open FSharp.Control
open FSharp.UMX
open MongoDB.Driver
open Shared
open Serilog
open System.Threading

type VehicleReadModelDto =
    { vehicleId: string
      make: string
      model: string
      year: int
      status: string }

module VehicleStatus =
    let available = "Available"
    let leased = "Leased"
    let removed = "Removed"

type VehicleReadModel(config:Config) =
    let name = "vehicles"
    let stream = UMX.tag<stream> "$ce-Vehicle"
    let store = Store(config.MongoConfig)
    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()
    let vehicleCollection = store.GetCollection(name)

    let addVehicle (vehicle:Vehicle) =
        async {
            Log.Debug("adding vehicle {@Vehicle}", vehicle)
            // use replace with upsert to make idempotent
            let vehicleId = VehicleId.toString vehicle.VehicleId
            let vehicleDto =
                { vehicleId = vehicleId
                  make = vehicle.Make
                  model = vehicle.Model
                  year = vehicle.Year
                  status = VehicleStatus.available }
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let replaceOptions = ReplaceOptions(IsUpsert = true)
            do! vehicleCollection.ReplaceOneAsync(filterVehicle, vehicleDto, replaceOptions)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let updateVehicle (vehicle:Vehicle) =
        async {
            Log.Debug("updating vehicle {@Vehicle}", vehicle)
            let vehicleId = VehicleId.toString vehicle.VehicleId
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateVehicle =
                Builders<VehicleReadModelDto>.Update
                    .Set((fun v -> v.make), vehicle.Make)
                    .Set((fun v -> v.model), vehicle.Model)
                    .Set((fun v -> v.year), vehicle.Year)
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let setVehicleStatus (vehicleId:VehicleId) (status:string) =
        async {
            Log.Debug("setting Vehicle-{VehicleId} status to {VehicleStatus}", status)
            let vehicleId = VehicleId.toString vehicleId
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateStatus = Builders<VehicleReadModelDto>.Update.Set((fun v -> v.status), status)
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateStatus)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let handleEvent: EventHandler =
        fun event ->
            async {
                let decodedEvent = codec.TryDecode event
                match decodedEvent with
                | Some vehicleEvent ->
                    match vehicleEvent with
                    | VehicleAdded vehicle ->
                        do! addVehicle vehicle
                    | VehicleUpdated vehicle ->
                        do! updateVehicle vehicle
                    | VehicleRemoved payload ->
                        let newStatus = VehicleStatus.removed
                        do! setVehicleStatus payload.VehicleId newStatus
                    | VehicleLeased payload ->
                        let newStatus = VehicleStatus.leased
                        do! setVehicleStatus payload.VehicleId newStatus
                    | VehicleReturned payload ->
                        let newStatus = VehicleStatus.available
                        do! setVehicleStatus payload.VehicleId newStatus
                | None -> ()
                do! store.UpdateCheckpoint(name, event.Index)
            }

    let subscription = Subscription(name, stream, handleEvent, config.EventStoreConfig)

    member _.StartAsync(cancellationToken:CancellationToken) =
        async {
            // ref: https://eventstore.com/docs/projections/system-projections/index.html?tabs=tabid-5#by-category
            let! rawCheckpoint = store.GetCheckpoint(name)
            Log.Debug("raw checkpoint: {RawCheckpoint}", rawCheckpoint)
            let checkpoint = 
                match rawCheckpoint with
                | Some checkpoint -> Checkpoint.StreamPosition checkpoint
                | None -> Checkpoint.StreamStart
            do! subscription.SubscribeAsync(checkpoint, cancellationToken)
        }
