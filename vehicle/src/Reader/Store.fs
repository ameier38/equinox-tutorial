namespace Reader

open MongoDB.Driver
open Shared

type Store(mongoConfig:MongoConfig) =
    let address = MongoServerAddress(mongoConfig.Host, mongoConfig.Port)
    let credential = MongoCredential.CreateCredential("admin", mongoConfig.User, mongoConfig.Password)
    let settings =
        MongoClientSettings(
            ConnectionMode=ConnectionMode.ReplicaSet,
            ReplicaSetName=mongoConfig.ReplicaSet,
            Server=address,
            Credential=credential)
    let mongo = MongoClient(settings)
    let db = mongo.GetDatabase("dealership")

    member _.GetCollection<'T>(name:string) = db.GetCollection<'T>(name)