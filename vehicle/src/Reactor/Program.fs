
open Reactor
open Serilog
open Serilog.Events
open System
open System.IO
open System.Threading

let checkHealth (mongo:Store.Mongo) (eventstore:Store.EventStore) (lockFilePath:string) = 
    let rec recurse () =
        async {
            let! mongoConnected = mongo.CheckConnectionAsync()
            if not mongoConnected then
                Log.Error("Could not connect to Mongo!")
                File.Delete(lockFilePath)
            let! eventstoreConnected = eventstore.CheckConnectionAsync()
            if not eventstoreConnected then
                Log.Error("Could not connect to EventStore!")
                File.Delete(lockFilePath)
            do! Async.Sleep(60 * 1000)
            do! recurse ()
        }
    recurse ()

[<EntryPoint>]
let main _ =
    let appName = "Vehicle Reactor"
    let debug = Shared.Env.getEnv "DEBUG" "false" |> bool.Parse
    let seqConfig = Shared.SeqConfig.Load()
    let logger =
        LoggerConfiguration()
            .Enrich.WithProperty("Application", appName)
            .MinimumLevel.Is(if debug then LogEventLevel.Debug else LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.Seq(seqConfig.Url)
            .CreateLogger()
    Log.Logger <- logger
    Log.Debug("🐛 Debug mode")
    let config = Config.Load(appName)
    Log.Debug("Config: {@Config}", config)
    Log.Debug("Initializing Mongo")
    let mongo = Store.Mongo(config.MongoConfig)
    Log.Debug("Initializing EventStore")
    let eventstore = Store.EventStore(config.EventStoreConfig)
    Log.Debug("Initializing reactor")
    let reactor = Reactor(mongo, eventstore)
    Log.Information("📜 Logs sent to {Url}", seqConfig.Url)
    Log.Information("🍃 Connected to MongoDB at {Host}", config.MongoConfig.Url)
    Log.Information("🐲 Connected to EventStore at {Url}", config.EventStoreConfig.Url)
    Log.Information("🚀 Starting vehicle reactor")
    use cancellation = new CancellationTokenSource()
    Console.CancelKeyPress |> Event.add (fun _ ->
        Log.Information("shutting down...")
        cancellation.Cancel())
    let lockFilePath = "/tmp/.lock"
    Log.Information("Writing lock file {LockFilePath}", lockFilePath)
    File.WriteAllText(lockFilePath, String.Empty)
    let healthCheck = checkHealth mongo eventstore lockFilePath
    Async.Start(healthCheck, cancellation.Token)
    reactor.StartAsync(cancellation.Token)
    |> Async.RunSynchronously
    Log.CloseAndFlush()
    0 // return an integer exit code
