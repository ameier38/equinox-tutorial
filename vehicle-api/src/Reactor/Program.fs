
open Reactor
open Serilog
open Serilog.Events
open System
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
    let vehicleReadModel = ReadModel.VehicleReadModel(config)
    Log.Information("📜 Logs sent to {Url}", config.SeqConfig.Url)
    Log.Information("🚀 Starting vehicle read model")
    use cancellation = new CancellationTokenSource()
    Console.CancelKeyPress |> Event.add (fun _ ->
        Log.Information("shutting down...")
        cancellation.Cancel())
    vehicleReadModel.StartAsync(cancellation.Token)
    |> Async.RunSynchronously
    Log.CloseAndFlush()
    0 // return an integer exit code
