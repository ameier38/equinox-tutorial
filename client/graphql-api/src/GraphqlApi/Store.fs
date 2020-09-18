namespace GraphqlApi

open MongoDB.Driver
open Serilog

type Store(mongoConfig:MongoConfig) =
    let address = MongoServerAddress(mongoConfig.Host, mongoConfig.Port)
    let credential = MongoCredential.CreateCredential("admin", mongoConfig.User, mongoConfig.Password)
    let settings = MongoClientSettings(Credential = credential, Server = address)
    let mongo = MongoClient(settings)
    let db = mongo.GetDatabase("dealership")
    do Log.Information("🍃 Connected to MongoDB at {Url}", mongoConfig.Url)

    member _.GetCollection<'T>(name:string) = db.GetCollection<'T>(name)
