namespace Server

open Equinox.EventStore
open Shared
open System

type Store(config:Config, log:Serilog.ILogger) =
    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()
    let getVehicleStreamName (vehicleId:VehicleId) =
        FsCodec.StreamName.create "Vehicle" (VehicleId.toStringN vehicleId)
    let cache = Equinox.Cache(config.AppName, 20) 
    let connector =
        Connector(
            username=config.EventStoreConfig.User,
            password=config.EventStoreConfig.Password,
            reqTimeout=TimeSpan.FromSeconds 5.,
            reqRetries=3,
            log=Logger.SerilogNormal log)
    let eventStoreConn =
        connector.Connect(config.AppName, Discovery.Uri(Uri(config.EventStoreConfig.Url)))
        |> Async.RunSynchronously
    let conn = Connection(eventStoreConn)
    let context = Context(conn, BatchingPolicy (maxBatchSize=500))
    let cacheStrategy = CachingStrategy.SlidingWindow(cache, TimeSpan.FromMinutes 20.)
    let resolver = Resolver(context, codec, Aggregate.fold, Aggregate.initial, cacheStrategy)

    member _.ResolveVehicle(vehicleId:VehicleId) =
        let vehicleStreamName = getVehicleStreamName vehicleId
        let vehicleStream = resolver.Resolve(vehicleStreamName)
        Equinox.Stream(log, vehicleStream, maxAttempts=3)
