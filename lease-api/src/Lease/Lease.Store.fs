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
    let strategy = ConnectionStrategy.ClusterTwinPreferSlaveReads
    let conn = 
        connector.Establish("lease", Discovery.Uri config.EventStore.DiscoveryUri, strategy)
        |> Async.RunSynchronously
    let gateway = Context(conn, BatchingPolicy(maxBatchSize=500))

    member __.Gateway with get () = gateway

type StreamResolver<'event,'state>
    (   store:Store,
        codec:Equinox.Codec.IUnionEncoder<'event,byte[]>,
        cachePrefix: string,
        fold:'state -> 'event seq -> 'state,
        initial:'state ) =
    let cache = Caching.Cache ("ES", 20)
    let cacheStrategy = CachingStrategy.SlidingWindowPrefixed (cache, TimeSpan.FromMinutes 20., cachePrefix)
    let resolver = Resolver(store.Gateway, codec, fold, initial, caching=cacheStrategy)

    member __.Resolve = resolver.Resolve
