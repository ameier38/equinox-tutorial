module Lease.Store

open Equinox.EventStore
open Serilog
open System

type Store(config:Config) =
    let timeout = TimeSpan.FromSeconds 5.0
    let log = Log.Logger |> Logger.SerilogNormal
    let connector = 
        GesConnector(
            config.EventStore.User, 
            config.EventStore.Password, 
            reqTimeout=timeout, 
            reqRetries=1, 
            log=log)
    let cache = Caching.Cache ("ES", 20)
    let strategy = ConnectionStrategy.ClusterTwinPreferSlaveReads
    let conn = 
        connector.Establish("lease", Discovery.Uri config.EventStore.Uri, strategy)
        |> Async.RunSynchronously
    let gateway = GesGateway(conn, GesBatchingPolicy(maxBatchSize=500))
    let cacheStrategy = Equinox.EventStore.CachingStrategy.SlidingWindow (cache, TimeSpan.FromMinutes 20.)
    let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
    let codec = Equinox.Codec.NewtonsoftJson.Json.Create<StoredEvent>(serializationSettings)
    let resolver = GesResolver(gateway, codec, Aggregate.fold, Aggregate.initialState, caching=cacheStrategy)

    member __.Resolve = resolver.Resolve
        
