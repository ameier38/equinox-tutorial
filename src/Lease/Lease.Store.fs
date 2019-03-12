module Lease.Store

open Equinox.EventStore
open Lease.Aggregate
open Serilog
open System

type Store(config:EventStoreConfig, aggregate:Aggregate) =
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
        connector.Establish(aggregate.Entity, Discovery.Uri uri, strategy)
        |> Async.RunSynchronously
    let gateway = GesGateway(conn, GesBatchingPolicy(maxBatchSize=500))
    let cacheStrategy = Equinox.EventStore.CachingStrategy.SlidingWindow (cache, TimeSpan.FromMinutes 20.)
    let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
    let codec = Equinox.UnionCodec.JsonUtf8.Create<LeaseEvent>(serializationSettings)
    let initial = List.empty
    let resolver = GesResolver(gateway, codec, aggregate.Fold, initial, caching=cacheStrategy)

    member __.Resolve = resolver.Resolve
        
