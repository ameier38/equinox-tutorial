namespace Reactor

open FSharp.Control
open FSharp.UMX
open MongoDB.Bson
open MongoDB.Driver
open Shared
open Serilog
open Shared.Dto
open System
open System.Threading

module VehicleStatus =
    let available = "Available"
    let leased = "Leased"
    let removed = "Removed"

type Reactor(documentStore:Store.DocumentStore, eventStore:Store.EventStore) =
    let name = "vehicles"
    // ref: https://eventstore.com/docs/projections/system-projections/index.html?tabs=tabid-5#by-category
    let stream = UMX.tag<stream> "$ce-Vehicle"
    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()
    let vehicleCollection = documentStore.GetCollection(name)
    let log = Log.ForContext<Reactor>()

    let addVehicle (session:IClientSessionHandle, timestamp:DateTimeOffset, vehicleId:VehicleId, vehicle:Vehicle) =
        async {
            log.Debug("adding vehicle {@Vehicle}", vehicle)
            // use replace with upsert to make idempotent
            let vehicleId = VehicleId.toStringN vehicleId
            let vehicleDto =
                { _id = ObjectId.Empty
                  vehicleId = vehicleId
                  addedAt = timestamp
                  updatedAt = timestamp
                  make = UMX.untag vehicle.Make
                  model = UMX.untag vehicle.Model
                  year = UMX.untag vehicle.Year
                  status = VehicleStatus.available
                  avatar = ""
                  images = [||] }
            let filterVehicle = Builders<Dto.InventoriedVehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let replaceOptions = ReplaceOptions(IsUpsert = true)
            do! vehicleCollection.ReplaceOneAsync(session, filterVehicle, vehicleDto, replaceOptions)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let updateVehicle (session:IClientSessionHandle, timestamp:DateTimeOffset, vehicleId:VehicleId, vehicle:Vehicle) =
        async {
            log.Debug("updating vehicle {@Vehicle}", vehicle)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<Dto.InventoriedVehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateVehicle =
                Builders<Dto.InventoriedVehicleDto>.Update
                    .Set((fun v -> v.updatedAt), timestamp)
                    .Set((fun v -> v.make), UMX.untag vehicle.Make)
                    .Set((fun v -> v.model), UMX.untag vehicle.Model)
                    .Set((fun v -> v.year), UMX.untag vehicle.Year)
            do! vehicleCollection.UpdateOneAsync(session, filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let addImage (session:IClientSessionHandle, timestamp:DateTimeOffset, vehicleId:VehicleId, image:Url) =
        async {
            log.Debug("adding image to Vehicle-{VehicleId}", vehicleId)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<Dto.InventoriedVehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            // TODO: use `nameof` in .NET 5
            let imagesField = FieldDefinition<Dto.InventoriedVehicleDto>.op_Implicit("images")
            let updateVehicle =
                Builders<Dto.InventoriedVehicleDto>.Update
                    .Set((fun v -> v.updatedAt), timestamp)
                    .AddToSet(imagesField, UMX.untag image)
            do! vehicleCollection.UpdateOneAsync(session, filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }
    
    let removeImage (session:IClientSessionHandle, timestamp:DateTimeOffset, vehicleId:VehicleId, imageUrl:Url) =
        async {
            log.Debug("removing image from Vehicle-{VehicleId}", vehicleId)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<Dto.InventoriedVehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            // TODO: use `nameof` in .NET 5
            let imageUrlsField = FieldDefinition<Dto.InventoriedVehicleDto>.op_Implicit("imageUrls")
            let updateVehicle =
                Builders<Dto.InventoriedVehicleDto>.Update
                    .Set((fun v -> v.updatedAt), timestamp)
                    .Pull(imageUrlsField, UMX.untag imageUrl)
            do! vehicleCollection.UpdateOneAsync(session, filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let updateAvatar (session:IClientSessionHandle, timestamp:DateTimeOffset, vehicleId:VehicleId, avatar:Url) =
        async {
            log.Debug("updating avatar of Vehicle-{VehicleId}", vehicleId)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<Dto.InventoriedVehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateVehicle =
                Builders<Dto.InventoriedVehicleDto>.Update
                    .Set((fun v -> v.updatedAt), timestamp)
                    .Set((fun v -> v.avatar), UMX.untag avatar)
            do! vehicleCollection.UpdateOneAsync(session, filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let removeAvatar (session:IClientSessionHandle, timestamp:DateTimeOffset, vehicleId:VehicleId) =
        async {
            log.Debug("updating avatar of Vehicle-{VehicleId}", vehicleId)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<Dto.InventoriedVehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateVehicle =
                Builders<Dto.InventoriedVehicleDto>.Update
                    .Set((fun v -> v.updatedAt), timestamp)
                    .Set((fun v -> v.avatar), "")
            do! vehicleCollection.UpdateOneAsync(session, filterVehicle, updateVehicle)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let setVehicleStatus (session:IClientSessionHandle, timestamp:DateTimeOffset, vehicleId:VehicleId, status:string) =
        async {
            log.Debug("setting Vehicle-{VehicleId} status to {VehicleStatus}", status)
            let vehicleId = VehicleId.toStringN vehicleId
            let filterVehicle = Builders<Dto.InventoriedVehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateStatus =
                Builders<Dto.InventoriedVehicleDto>.Update
                    .Set((fun v -> v.updatedAt), timestamp)
                    .Set((fun v -> v.status), status)
            do! vehicleCollection.UpdateOneAsync(session, filterVehicle, updateStatus)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let handleEvent =
        fun (event:FsCodec.ITimelineEvent<byte []>) ->
            let work (session:IClientSessionHandle) =
                async {
                    let timestamp = event.Timestamp
                    let decodedEvent = codec.TryDecode event
                    match decodedEvent with
                    | Some vehicleEvent ->
                        match vehicleEvent with
                        | VehicleAdded payload ->
                            do! addVehicle (session, timestamp, payload.VehicleId, payload.Vehicle)
                        | VehicleUpdated payload ->
                            do! updateVehicle (session, timestamp, payload.VehicleId, payload.Vehicle)
                        | ImageAdded payload ->
                            do! addImage (session, timestamp, payload.VehicleId, payload.ImageUrl) 
                        | ImageRemoved payload ->
                            do! removeImage (session, timestamp, payload.VehicleId, payload.ImageUrl)
                        | AvatarUpdated payload ->
                            do! updateAvatar (session, timestamp, payload.VehicleId, payload.AvatarUrl)
                        | AvatarRemoved payload ->
                            do! removeAvatar (session, timestamp, payload.VehicleId)
                        | VehicleRemoved payload ->
                            let newStatus = VehicleStatus.removed
                            do! setVehicleStatus (session, timestamp, payload.VehicleId, newStatus)
                        | VehicleLeased payload ->
                            let newStatus = VehicleStatus.leased
                            do! setVehicleStatus (session, timestamp, payload.VehicleId, newStatus)
                        | VehicleReturned payload ->
                            let newStatus = VehicleStatus.available
                            do! setVehicleStatus (session, timestamp, payload.VehicleId, newStatus)
                    | None -> ()
                    do! documentStore.UpdateCheckpointAsync(session, name, event.Index)
                }
            documentStore.TransactAsync(work)

    let subscription = Subscription(name, stream, handleEvent, eventStore)

    member _.StartAsync(cancellationToken:CancellationToken) =
        async {
            let! rawCheckpoint = documentStore.GetCheckpointAsync(name)
            log.Debug("raw checkpoint: {RawCheckpoint}", rawCheckpoint)
            let checkpoint = 
                match rawCheckpoint with
                | Some checkpoint -> Checkpoint.StreamPosition checkpoint
                | None -> Checkpoint.StreamStart
            do! subscription.SubscribeAsync(checkpoint, cancellationToken)
        }
