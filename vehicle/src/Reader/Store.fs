namespace Reader

open MongoDB.Driver
open MongoDB.Bson
open Shared
open Serilog

type Store(mongoConfig:MongoConfig) =
    let user = Secret.value mongoConfig.User
    let password = Secret.value mongoConfig.Password
    let credential = MongoCredential.CreateCredential("admin", user, password)
    let servers =
        mongoConfig.Host.Split(",")
        |> Array.map (fun host -> MongoServerAddress(host, mongoConfig.Port))
    let settings =
        MongoClientSettings(
            ConnectionMode=ConnectionMode.ReplicaSet,
            ReplicaSetName=mongoConfig.ReplicaSet,
            Servers=servers,
            Credential=credential)
    do Log.Debug("Connectiong to Mongo at {Url}", mongoConfig.Url)
    let mongo = MongoClient(settings)
    let db = mongo.GetDatabase("dealership")

    member _.GetCollection<'T>(name:string) = db.GetCollection<'T>(name)

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
