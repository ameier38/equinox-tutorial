open Grpc.Core
open Grpc.Health.V1
open Grpc.HealthCheck
open Vehicle
open Serilog
open System
open System.Threading

[<EntryPoint>]
let main _ =
    let getUtcNow () = DateTimeOffset.UtcNow
    let config = Config.Load()
    let log =
        LoggerConfiguration()
            .Enrich.WithProperty("Application", config.AppName)
            .WriteTo.Console()
            .WriteTo.Seq(config.Seq.Url)
            .CreateLogger()
    let store = Store(config, log)
    let vehicleService = VehicleServiceImpl(store)
    let healthService = HealthServiceImpl()
    let server = Server()
    server.Services.Add(Tutorial.Vehicle.V1.VehicleService.BindService(vehicleService))
    server.Services.Add(Health.BindService(healthService))
    let serverPort = ServerPort(config.Server.Host, config.Server.Port, ServerCredentials.Insecure)
    let listenPort = server.Ports.Add(serverPort)
    server.Start()
    log.Information("🚀 Server listening at :{Port}", listenPort)
    log.Information("🐲 Connected to EventStore at {Url}", config.EventStore.DiscoveryUri)
    log.Information("📜 Logs available at {Url}", config.Seq.Url)
    let exitEvent = new AutoResetEvent(false)
    let exitHandler = new ConsoleCancelEventHandler(fun _ _ -> exitEvent.Set() |> ignore)
    Console.CancelKeyPress.AddHandler(exitHandler)
    exitEvent.WaitOne() |> ignore
    server.ShutdownAsync().Wait()
    Log.CloseAndFlush()
    0 // return an integer exit code
