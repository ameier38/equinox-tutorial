module Reactor.ReadModel

open FSharp.Control
open FSharp.UMX
open FSharp.ValidationBlocks
open MongoDB.Driver
open Shared
open Serilog
open System
open System.Threading

type VehicleReadModelDto =
    { vehicleId: string
      make: string
      model: string
      year: int
      avatarUri: string
      imageUris: string array
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

    let addVehicle (vehicleId:VehicleId, vehicle:Vehicle) =
        async {
            Log.Debug("adding vehicle {@Vehicle}", vehicle)
            // use replace with upsert to make idempotent
            let vehicleId = VehicleId.toStringN vehicleId
            let vehicleDto =
                { vehicleId = vehicleId
                  make = Block.value vehicle.Make
                  model = Block.value vehicle.Model
                  year = Block.value vehicle.Year
                  avatarUri = ""
                  imageUris = [||]
                  status = VehicleStatus.available }
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let replaceOptions = ReplaceOptions(IsUpsert = true)
            do! vehicleCollection.ReplaceOneAsync(filterVehicle, vehicleDto, replaceOptions)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let updateVehicle (vehicleId:VehicleId, vehicle:Vehicle) =
        async {
            Log.Debug("updating vehicle {@Vehicle}", vehicle)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateVehicle =
                Builders<VehicleReadModelDto>.Update
                    .Set((fun v -> v.make), (Block.value vehicle.Make))
                    .Set((fun v -> v.model), (Block.value vehicle.Model))
                    .Set((fun v -> v.year), (Block.value vehicle.Year))
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let addImage (vehicleId:VehicleId, imageUri:Uri) =
        async {
            Log.Debug("adding image to Vehicle-{VehicleId}", vehicleId)
            let vehicleId = VehicleId.toStringN vehicleId
            let imageUri = imageUri.AbsoluteUri
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            // TODO: use `nameof` in .NET 5
            let imageUrisField = FieldDefinition<VehicleReadModelDto>.op_Implicit("imageUris")
            let updateVehicle =
                Builders<VehicleReadModelDto>.Update
                    .AddToSet(imageUrisField, imageUri)
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }
    
    let removeImage (vehicleId:VehicleId, imageUri:Uri) =
        async {
            Log.Debug("adding image to Vehicle-{VehicleId}", vehicleId)
            let vehicleId = VehicleId.toStringN vehicleId
            let imageUri = imageUri.AbsoluteUri
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            // TODO: use `nameof` in .NET 5
            let imageUrisField = FieldDefinition<VehicleReadModelDto>.op_Implicit("imageUris")
            let updateVehicle =
                Builders<VehicleReadModelDto>.Update
                    .Pull(imageUrisField, imageUri)
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let updateAvatar (vehicleId:VehicleId, avatarUri:Uri) =
        async {
            Log.Debug("updating avatar of Vehicle-{VehicleId}", vehicleId)
            let vehicleId = VehicleId.toStringN vehicleId
            let avatarUri = avatarUri.AbsoluteUri
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateVehicle =
                Builders<VehicleReadModelDto>.Update
                    .Set((fun v -> v.avatarUri), avatarUri)
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let removeAvatar (vehicleId:VehicleId) =
        async {
            Log.Debug("updating avatar of Vehicle-{VehicleId}", vehicleId)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateVehicle =
                Builders<VehicleReadModelDto>.Update
                    .Set((fun v -> v.avatarUri), "")
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let setVehicleStatus (vehicleId:VehicleId) (status:string) =
        async {
            Log.Debug("setting Vehicle-{VehicleId} status to {VehicleStatus}", status)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<VehicleReadModelDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateStatus = Builders<VehicleReadModelDto>.Update.Set((fun v -> v.status), status)
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateStatus)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let handleEvent =
        fun event ->
            async {
                let decodedEvent = codec.TryDecode event
                match decodedEvent with
                | Some vehicleEvent ->
                    match vehicleEvent with
                    | VehicleAdded payload ->
                        do! addVehicle (payload.VehicleId, payload.Vehicle)
                    | VehicleUpdated payload ->
                        do! updateVehicle (payload.VehicleId, payload.Vehicle)
                    | ImageAdded payload ->
                        do! addImage (payload.VehicleId, payload.ImageUri) 
                    | ImageRemoved payload ->
                        do! removeImage (payload.VehicleId, payload.ImageUri)
                    | AvatarUpdated payload ->
                        do! updateAvatar (payload.VehicleId, payload.AvatarUri)
                    | AvatarRemoved payload ->
                        do! removeAvatar payload.VehicleId
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
