module Lease.Store

open Equinox.EventStore
open Serilog
open System

type Store(config:EventStoreConfig) =
    let uri = 
        sprintf "%s://@%s:%d" 
            config.Protocol 
            config.Host 
            config.Port
        |> Uri
    let timeout = TimeSpan.FromSeconds 5.0
    let log = Log.Logger |> Logger.SerilogNormal
    let connector = 
        GesConnector(
            config.User, 
            config.Password, 
            reqTimeout=timeout, 
            reqRetries=1, 
            log=log)
    let cache = Caching.Cache ("ES", 20)
    let strategy = ConnectionStrategy.ClusterTwinPreferSlaveReads
    let conn = 
        connector.Establish("lease", Discovery.Uri uri, strategy)
        |> Async.RunSynchronously
    let gateway = GesGateway(conn, GesBatchingPolicy(maxBatchSize=500))
    let cacheStrategy = Equinox.EventStore.CachingStrategy.SlidingWindow (cache, TimeSpan.FromMinutes 20.)
    let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
    let codec = Equinox.Codec.NewtonsoftJson.Json.Create<StoredEvent>(serializationSettings)
    let initial =
        { NextEventId = 0<eventId>
          LeaseEvents = []
          DeletedEvents = [] }
    let resolver = GesResolver(gateway, codec, Aggregate.fold, initial, caching=cacheStrategy)

    member __.Resolve = resolver.Resolve
        
