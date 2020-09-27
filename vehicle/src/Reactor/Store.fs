module Reactor.Store

open EventStore.ClientAPI
open MongoDB.Driver
open MongoDB.Bson
open Serilog
open Shared
open System

type CheckpointDto =
    { _id: ObjectId
      model: string
      checkpoint: int64 }

type DocumentStore(mongoConfig:MongoConfig) =
    let checkpointCollectionName = "checkpoints"
    let address = MongoServerAddress(mongoConfig.Host, mongoConfig.Port)
    let credential = MongoCredential.CreateCredential("admin", mongoConfig.User, mongoConfig.Password)
    let settings = MongoClientSettings(Credential = credential, Server = address)
    let mongo = MongoClient(settings)
    let db = mongo.GetDatabase("dealership")
    let checkpointCollection = db.GetCollection<CheckpointDto>(checkpointCollectionName)
    let log = Log.ForContext<DocumentStore>()

    member _.GetCheckpointAsync(model:string): Async<int64 option> =
        async {
            log.Debug("getting checkpoint for {Model}", model)
            let! checkpoint =
                checkpointCollection
                    .Find(fun doc -> doc.model = model)
                    .FirstOrDefaultAsync()
                |> Async.AwaitTask
            return
                if isNull (box checkpoint) then None
                else Some checkpoint.checkpoint
        }

    member _.UpdateCheckpointAsync(session:IClientSessionHandle, model:string, checkpoint:int64) =
        async {
            log.Debug("updating {Model} checkpoint to {Checkpoint}", model, checkpoint)
            let filterCheckpoint = Builders<CheckpointDto>.Filter.Where(fun cp -> cp.model = model)
            let updateCheckpoint = Builders<CheckpointDto>.Update.Set((fun cp -> cp.checkpoint), checkpoint)
            let updateOptions = UpdateOptions(IsUpsert = true)
            do! checkpointCollection.UpdateOneAsync(session, filterCheckpoint, updateCheckpoint, updateOptions)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    member _.GetCollection<'T>(name:string) = db.GetCollection<'T>(name)

    member _.TransactAsync(work:IClientSessionHandle -> Async<unit>) =
        async {
            use session = mongo.StartSession()
            try
                session.StartTransaction()
                do! work session
                session.CommitTransaction()
            with ex ->
                log.Error(ex, "Error executing transaction")
                session.AbortTransaction()
        }

type EventStore(eventstoreConfig:EventStoreConfig) =
    let conn = EventStoreConnection.Create(Uri(eventstoreConfig.Url))
    do conn.ConnectAsync().Wait()

    member _.Connection with get() = conn

    member _.Credentials with get() =
        SystemData.UserCredentials(
            username = eventstoreConfig.User,
            password = eventstoreConfig.Password)
