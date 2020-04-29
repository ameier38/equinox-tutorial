module Vehicle.Store

open Equinox.EventStore
open System

let streamName (vehicleId:VehicleId) = FsCodec.StreamName.create "Vehicle" (Guid.toStringN vehicleId)
let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()

type IStore =
    abstract member Resolve: VehicleId -> Equinox.Stream<VehicleEvent, VehicleState>

type Store(config:Config, log:Serilog.ILogger) =
    let cache = Equinox.Cache(config.AppName, 20) 
    let connector =
        Connector (
            username=config.EventStore.User,
            password=config.EventStore.Password,
            reqTimeout=TimeSpan.FromSeconds 5.,
            reqRetries=3,
            log=Logger.SerilogNormal log)
    let eventStoreConn =
        connector.Connect(config.AppName, Discovery.GossipDns config.EventStore.Host)
        |> Async.RunSynchronously
    let conn = Connection (eventStoreConn)
    let context = Context (conn, BatchingPolicy (maxBatchSize=500))
    let cacheStrategy = CachingStrategy.SlidingWindow (cache, TimeSpan.FromMinutes 20.)
    let resolver = Resolver(context, Aggregate.codec, Aggregate.fold, Aggregate.initial, cacheStrategy)
    let resolve vehicleId = Equinox.Stream(log, resolver.Resolve(streamName vehicleId), maxAttempts=3)

    member _.Resolve = resolve
