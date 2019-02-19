namespace Dog

open Serilog
open System

/// Equinox store bindings
module Storage =
    /// Specifies the store to be used, together with any relevant custom parameters
    [<RequireQualifiedAccess>]
    type Config =
        | Mem
        | ES of host: string * username: string * password: string * cacheMb: int

    /// Holds an initialized/customized/configured of the store as defined by the `Config`
    type Instance =
        | MemoryStore of Equinox.MemoryStore.VolatileStore
        | EventStore of gateway: Equinox.EventStore.GesGateway * cache: Equinox.EventStore.Caching.Cache

    /// MemoryStore 'wiring', uses Equinox.MemoryStore nuget package
    module private Memory =
        open Equinox.MemoryStore
        let connect () =
            VolatileStore()

    /// EventStore wiring, uses Equinox.EventStore nuget package
    module private ES =
        open Equinox.EventStore
        let mkCache mb = Caching.Cache ("ES", mb)
        let connect host username password =
            let log = Logger.SerilogNormal (Log.ForContext<Instance>())
            let connector = GesConnector(username, password, reqTimeout=TimeSpan.FromSeconds 5., reqRetries=1, log=log)
            let conn = 
                connector.Establish ("Twin", Discovery.GossipDns host, ConnectionStrategy.ClusterTwinPreferSlaveReads) 
                |> Async.RunSynchronously
            GesGateway(conn, GesBatchingPolicy(maxBatchSize=500))

    /// Creates and/or connects to a specific store as dictated by the specified config
    let connect : Config -> Instance = function
        | Config.Mem ->
            let store = Memory.connect()
            Instance.MemoryStore store
        | Config.ES (host, user, pass, cache) ->
            let cache = ES.mkCache cache
            let conn = ES.connect host user pass
            Instance.EventStore (conn, cache)

