module Lease.Store

open Equinox.EventStore
open Serilog
open System

type Store(config:Config) =
    let timeout = TimeSpan.FromSeconds 5.0
    let log = Log.Logger |> Logger.SerilogNormal
    let creds = EventStore.ClientAPI.SystemData.UserCredentials(config.EventStore.User, config.EventStore.Password)
    let connector = 
        Connector(
            config.EventStore.User, 
            config.EventStore.Password, 
            reqTimeout=timeout, 
            reqRetries=1, 
            log=log)
    let strategy = ConnectionStrategy.ClusterTwinPreferSlaveReads
    let conn = 
        connector.Establish("Twin", Discovery.Uri config.EventStore.DiscoveryUri, strategy)
        |> Async.RunSynchronously
    let context = Context(conn, BatchingPolicy(maxBatchSize=500))
    let cache = Caching.Cache ("ES", 100)

    member __.Cache with get () = cache
    member __.Context with get () = context
    member __.ReadStream(streamName, start, count) =
        conn.ReadConnection.ReadStreamEventsForwardAsync(streamName, start, count, true, creds)
        |> Async.AwaitTask

type StreamResolver<'event,'state>
    (   store:Store,
        codec:FsCodec.IUnionEncoder<'event,byte[]>,
        cachePrefix: string,
        fold:'state -> 'event seq -> 'state,
        initial:'state ) =
    let cacheStrategy = CachingStrategy.SlidingWindowPrefixed (store.Cache, TimeSpan.FromMinutes 20., cachePrefix)
    let resolver = Resolver(store.Context, codec, fold, initial, caching=cacheStrategy)

    member __.Resolve = resolver.Resolve
