open Grpc.Core
open Grpc.Health.V1
open Grpc.HealthCheck
open Server
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
            .WriteTo.Seq(config.Seq.Url)
            .CreateLogger()
    Log.Logger <- logger
    let store = Store(config, logger)
    let vehicleService = VehicleServiceImpl(store)
    let healthService = HealthServiceImpl()
    let server = Server()
    server.Services.Add(CosmicDealership.Vehicle.V1.VehicleService.BindService(vehicleService))
    server.Services.Add(Health.BindService(healthService))
    let serverPort = ServerPort(config.Server.Host, config.Server.Port, ServerCredentials.Insecure)
    let listenPort = server.Ports.Add(serverPort)
    server.Start()
    Log.Information("🚀 Server listening at :{Port}", listenPort)
    Log.Information("🔗 Connected to EventStore at {Url}", config.EventStore.DiscoveryUri)
    Log.Information("📜 Logs sent to {Url}", config.Seq.Url)
    let exitEvent = new AutoResetEvent(false)
    let exitHandler = new ConsoleCancelEventHandler(fun _ _ -> exitEvent.Set() |> ignore)
    Console.CancelKeyPress.AddHandler(exitHandler)
    exitEvent.WaitOne() |> ignore
    server.ShutdownAsync().Wait()
    Log.CloseAndFlush()
    0 // return an integer exit code
