module Lease.Store

open Equinox.EventStore
open Serilog
open System

type Store(config:Config) =
    let timeout = TimeSpan.FromSeconds 5.0
    let log = Log.Logger |> Logger.SerilogNormal
    let connector = 
        Connector(
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
    let gateway = Context(conn, BatchingPolicy(maxBatchSize=500))

    member __.Gateway with get () = gateway
    member __.Cache with get () = cache

type StreamResolver<'event,'state>
    (   store:Store,
        codec:Equinox.Codec.IUnionEncoder<'event,byte[]>,
        fold:'state -> 'event seq -> 'state,
        initial:'state ) =
    let cacheStrategy = CachingStrategy.SlidingWindow (store.Cache, TimeSpan.FromMinutes 20.)
    let resolver = Resolver(store.Gateway, codec, fold, initial, caching=cacheStrategy)

    member __.Resolve = resolver.Resolve
