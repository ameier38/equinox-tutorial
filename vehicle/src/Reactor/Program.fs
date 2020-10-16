
open Reactor
open Serilog
open Serilog.Events
open System
open System.IO
open System.Threading

[<EntryPoint>]
let main _ =
    let getUtcNow () = DateTimeOffset.UtcNow
    let config = Config.Load()
    let logger =
        LoggerConfiguration()
            .Enrich.WithProperty("Application", config.AppName)
            .MinimumLevel.Is(if config.Debug then LogEventLevel.Debug else LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.Seq(config.SeqConfig.Url)
            .CreateLogger()
    Log.Logger <- logger
    Log.Debug("🐛 Debug mode")
    let documentStore = Store.DocumentStore(config.MongoConfig)
    let eventStore = Store.EventStore(config.EventStoreConfig)
    let reactor = Reactor(documentStore, eventStore)
    Log.Information("📜 Logs sent to {Url}", config.SeqConfig.Url)
    Log.Information("🍃 Connected to MongoDB at {Url}", config.MongoConfig.Url)
    Log.Information("🐲 Connected to EventStore at {Url}", config.EventStoreConfig.Url)
    Log.Information("🚀 Starting vehicle reactor")
    use cancellation = new CancellationTokenSource()
    Console.CancelKeyPress |> Event.add (fun _ ->
        Log.Information("shutting down...")
        cancellation.Cancel())
    let lockFile = "/tmp/.lock"
    try
        File.WriteAllText(lockFile, String.Empty)
        reactor.StartAsync(cancellation.Token)
        |> Async.RunSynchronously
    with ex ->
        File.Delete(lockFile)
        Log.Error(ex, "Error running application")
        Log.CloseAndFlush()
        raise ex
    Log.CloseAndFlush()
    0 // return an integer exit code
