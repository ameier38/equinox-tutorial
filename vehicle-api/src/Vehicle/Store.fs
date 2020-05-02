namespace Vehicle

open Equinox.EventStore
open System

type Store(config:Config, log:Serilog.ILogger) =
    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()
    let getVehicleStreamName (vehicleId:VehicleId) =
        FsCodec.StreamName.create "Vehicle" (Guid.toStringN vehicleId)
    // ref: https://eventstore.com/docs/projections/system-projections/index.html?tabs=tabid-5#by-event-type
    let cache = Equinox.Cache(config.AppName, 20) 
    let credentials =
        EventStore.ClientAPI.SystemData.UserCredentials(
            username=config.EventStore.User,
            password=config.EventStore.Password)
    let connector =
        Connector(
            username=config.EventStore.User,
            password=config.EventStore.Password,
            reqTimeout=TimeSpan.FromSeconds 5.,
            reqRetries=3,
            log=Logger.SerilogNormal log)
    let eventStoreConn =
        connector.Connect(config.AppName, Discovery.Uri config.EventStore.DiscoveryUri)
        |> Async.RunSynchronously
    let conn = Connection(eventStoreConn)
    let context = Context(conn, BatchingPolicy (maxBatchSize=500))
    let cacheStrategy = CachingStrategy.SlidingWindow(cache, TimeSpan.FromMinutes 20.)
    let resolver = Resolver(context, codec, Aggregate.fold, Aggregate.initial, cacheStrategy)
    let resolveVehicle vehicleId =
        let vehicleStreamName = getVehicleStreamName vehicleId
        let vehicleStream = resolver.Resolve(vehicleStreamName)
        Equinox.Stream(log, vehicleStream, maxAttempts=3)
    let readStream (streamName:string, pageToken:PageToken, pageSize:PageSize) =
        let start = pageToken |> PageToken.decode
        let pageSize = pageSize |> PageSize.toInt
        conn.ReadConnection.ReadStreamEventsForwardAsync(
            stream=streamName,
            start=start,
            count=pageSize,
            resolveLinkTos=true,
            userCredentials=credentials)
        |> Async.AwaitTask
    let tryDecodeEvent (resolvedEvent:EventStore.ClientAPI.ResolvedEvent) =
        resolvedEvent
        |> UnionEncoderAdapters.encodedEventOfResolvedEvent
        |> codec.TryDecode

    member _.ResolveVehicle = resolveVehicle

    member _.ReadStream = readStream

    member _.TryDecodeEvent = tryDecodeEvent
