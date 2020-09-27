open Grpc.Core
open Grpc.Health.V1
open Grpc.HealthCheck
open Reader
open Serilog
open Serilog.Events
open Shared
open System
open System.Threading

[<EntryPoint>]
let main argv =
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
    let vehicleQueryService = VehicleQueryServiceImpl(store)
    let healthService = HealthServiceImpl()
    let server = Server()
    server.Services.Add(CosmicDealership.Vehicle.V1.VehicleQueryService.BindService(vehicleQueryService))
    server.Services.Add(Health.BindService(healthService))
    let serverPort = ServerPort(config.ServerConfig.Host, config.ServerConfig.Port, ServerCredentials.Insecure)
    let listenPort = server.Ports.Add(serverPort)
    server.Start()
    Log.Information("🚀 Server listening at :{Port}", listenPort)
    Log.Information("🍃 Connected to MongoDB at {Url}", config.MongoConfig.Url)
    Log.Information("📜 Logs sent to {Url}", config.SeqConfig.Url)
    let exitEvent = new AutoResetEvent(false)
    let exitHandler = new ConsoleCancelEventHandler(fun _ _ -> exitEvent.Set() |> ignore)
    Console.CancelKeyPress.AddHandler(exitHandler)
    exitEvent.WaitOne() |> ignore
    server.ShutdownAsync().Wait()
    Log.CloseAndFlush()
    0 // return an integer exit code
