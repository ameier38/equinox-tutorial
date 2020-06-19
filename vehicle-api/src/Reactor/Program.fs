
open System
open Serilog
open Serilog.Events
open Reactor

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
    let store = Store(config.MongoConfig)
    let subscription = Subscription(config.EventStoreConfig, store)
    Log.Information("📜 Logs sent to {Url}", config.SeqConfig.Url)
    Log.Information("📨 Starting subscription")
    subscription.Start()
    |> Async.RunSynchronously
    0 // return an integer exit code
