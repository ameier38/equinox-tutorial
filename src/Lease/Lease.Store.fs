namespace Lease

open Equinox.EventStore
open FSharp.UMX
open Serilog
open System

module Store =
    let connect 
        (config:EventStoreConfig) 
        (aggregate:Aggregate<LeaseCommand,LeaseEvent,LeaseState>) =
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
        let accessStrategy = Equinox.EventStore.AccessStrategy.RollingSnapshots (aggregate.isOrigin, aggregate.compact)
        let cacheStrategy = Equinox.EventStore.CachingStrategy.SlidingWindow (cache, TimeSpan.FromMinutes 20.)
        let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
        let codec = Equinox.UnionCodec.JsonUtf8.Create<LeaseEvent>(serializationSettings)
        let initial = { NextId = %0; Events = [] }
        let fold = Seq.fold aggregate.evolve
        GesResolver(gateway, codec, fold, initial, accessStrategy, cacheStrategy)