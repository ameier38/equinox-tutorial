namespace Reactor

open MongoDB.Driver
open Serilog

type Store(mongoConfig:MongoConfig) =
    let vehicleCollectionName = "vehicles"
    let checkpointCollectionName = "checkpoints"
    let mongo = MongoClient(mongoConfig.Url)
    let db = mongo.GetDatabase("dealership")
    let vehicleCollection = db.GetCollection<VehicleDto>(vehicleCollectionName)
    let checkpointCollection = db.GetCollection<CheckpointDto>(checkpointCollectionName)

    let getCheckpoint model =
        async {
            let! checkpoint =
                checkpointCollection
                    .Find(fun doc -> doc.model = model)
                    .Project(fun doc -> doc.checkpoint)
                    .FirstOrDefaultAsync()
                |> Async.AwaitTask
            return
                if isNull (checkpoint :> obj) then 0L
                else checkpoint
        }

    let updateCheckpoint (model:string) (checkpoint:int64) =
        async {
            Log.Information("updating {Model} checkpoint to {Checkpoint}", model, checkpoint)
            let filterCheckpoint = Builders<CheckpointDto>.Filter.Where(fun cp -> cp.model = model)
            let updateCheckpoint = Builders<CheckpointDto>.Update.Set((fun cp -> cp.checkpoint), checkpoint)
            let updateOptions = UpdateOptions(IsUpsert = true)
            do! checkpointCollection.UpdateOneAsync(filterCheckpoint, updateCheckpoint, updateOptions)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    member _.GetVehicleCheckpoint(): Async<int64> = getCheckpoint vehicleCollectionName

    member _.AddVehicle(checkpoint:int64, vehicle:VehicleDto): Async<unit> =
        async {
            // use replace with upsert to make idempotent
            let filterVehicle = Builders<VehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicle.vehicleId)
            let replaceOptions = ReplaceOptions(IsUpsert = true)
            do! vehicleCollection.ReplaceOneAsync(filterVehicle, vehicle, replaceOptions)
                |> Async.AwaitTask
                |> Async.Ignore
            do! updateCheckpoint vehicleCollectionName checkpoint
        }

    member _.SetVehicleStatus(checkpoint:int64, vehicleId:string, status:string): Async<unit> =
        async {
            let filterVehicle = Builders<VehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateStatus = Builders<VehicleDto>.Update.Set((fun v -> v.status), status)
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateStatus)
                |> Async.AwaitTask
                |> Async.Ignore
            do! updateCheckpoint vehicleCollectionName checkpoint
        }
