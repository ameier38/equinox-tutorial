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

let private getCollection<'T> (log:ILogger) (db:IMongoDatabase) (name:string) : IMongoCollection<'T> =
    // NB: transactions require the collection to exist first
    try
        log.Information("Trying to create collection {Collection}", name)
        db.CreateCollection(name)
    with
    | :? TimeoutException as ex ->
        log.Error(ex, "Could not connect to Mongo")
        raise ex
    | ex ->
        log.Debug("{Collection} already exists: {@Error}", name, ex)
    db.GetCollection<'T>(name)

type Mongo(mongoConfig:MongoConfig) =
    let log = Log.ForContext<Mongo>()
    let databaseName = mongoConfig.Database
    let checkpointCollectionName = "checkpoints"
    let servers =
        mongoConfig.Host.Split(",")
        |> Array.map (fun host -> MongoServerAddress(host, mongoConfig.Port))
    let user = Secret.value mongoConfig.User
    let password = Secret.value mongoConfig.Password
    let credential = MongoCredential.CreateCredential("admin", user, password)
    let settings =
        MongoClientSettings(
            ConnectionMode=ConnectionMode.ReplicaSet,
            ReplicaSetName=mongoConfig.ReplicaSet,
            Servers=servers,
            Credential=credential)
    do log.Debug("Connecting to Mongo at {Url}", mongoConfig.Url)
    let mongo = MongoClient(settings)
    do log.Information("Getting database {Database}", databaseName)
    let db = mongo.GetDatabase(databaseName)
    let checkpointCollection = getCollection<CheckpointDto> log db checkpointCollectionName

    member _.CheckConnectionAsync() =
        async {
            try
                do!
                    let command = Command<BsonDocument>.op_Implicit("{ping:1}")
                    db.RunCommandAsync(command)
                    |> Async.AwaitTask
                    |> Async.Ignore
                return true
            with ex ->
                Log.Error(ex, "Could not connnect to Mongo")
                return false
        }

    member _.GetCheckpointAsync(model:string): Async<int64 option> =
        async {
            log.Debug("Getting checkpoint for {Model}", model)
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
            log.Debug("Updating {Model} checkpoint to {Checkpoint}", model, checkpoint)
            let filterCheckpoint = Builders<CheckpointDto>.Filter.Where(fun cp -> cp.model = model)
            let updateCheckpoint = Builders<CheckpointDto>.Update.Set((fun cp -> cp.checkpoint), checkpoint)
            let updateOptions = UpdateOptions(IsUpsert = true)
            do! checkpointCollection.UpdateOneAsync(session, filterCheckpoint, updateCheckpoint, updateOptions)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    member _.GetCollection<'T>(name:string) = getCollection<'T> log db name

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
    let log = Log.ForContext<EventStore>()
    let conn = EventStoreConnection.Create(Uri(eventstoreConfig.Url))
    do conn.ConnectAsync().Wait()
    let user = Secret.value eventstoreConfig.User
    let password = Secret.value eventstoreConfig.Password
    let creds = SystemData.UserCredentials(username = user, password = password)

    member _.Connection with get() = conn

    member _.Credentials with get() = creds

    member _.CheckConnectionAsync() =
        async {
            try
                do!
                    conn.ReadAllEventsForwardAsync(EventStore.ClientAPI.Position.Start, 1, false, creds)
                    |> Async.AwaitTask
                    |> Async.Ignore
                return true
            with ex ->
                log.Error(ex, "could not connnect to EventStore")
                return false
        }
