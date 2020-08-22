namespace Reactor

open MongoDB.Driver
open MongoDB.Bson
open Serilog

type CheckpointDto =
    { _id: ObjectId
      model: string
      checkpoint: int64 }

type Store(mongoConfig:MongoConfig) =
    let checkpointCollectionName = "checkpoints"
    let address = MongoServerAddress(mongoConfig.Host, mongoConfig.Port)
    let credential = MongoCredential.CreateCredential("admin", mongoConfig.User, mongoConfig.Password)
    let settings = MongoClientSettings(Credential = credential, Server = address)
    let mongo = MongoClient(settings)
    let db = mongo.GetDatabase("dealership")
    let checkpointCollection = db.GetCollection<CheckpointDto>(checkpointCollectionName)
    do Log.Information("🍃 Connected to MongoDB at {Url}", mongoConfig.Url)

    member _.GetCheckpoint(model:string): Async<int64 option> =
        async {
            Log.Debug("getting checkpoint for {Model}", model)
            let! checkpoint =
                checkpointCollection
                    .Find(fun doc -> doc.model = model)
                    .FirstOrDefaultAsync()
                |> Async.AwaitTask
            return
                if isNull (box checkpoint) then None
                else Some checkpoint.checkpoint
        }

    member _.UpdateCheckpoint(model:string, checkpoint:int64) =
        async {
            Log.Debug("updating {Model} checkpoint to {Checkpoint}", model, checkpoint)
            let filterCheckpoint = Builders<CheckpointDto>.Filter.Where(fun cp -> cp.model = model)
            let updateCheckpoint = Builders<CheckpointDto>.Update.Set((fun cp -> cp.checkpoint), checkpoint)
            let updateOptions = UpdateOptions(IsUpsert = true)
            do! checkpointCollection.UpdateOneAsync(filterCheckpoint, updateCheckpoint, updateOptions)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    member _.GetCollection<'T>(name:string) = db.GetCollection<'T>(name)
