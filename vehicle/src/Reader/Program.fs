open Grpc.Core
open Grpc.Health.V1
open Grpc.HealthCheck
open Reader
open Serilog
open Serilog.Events
open Shared
open System
open System.Threading

let checkHealth (store:Store) (healthService:HealthServiceImpl) = 
    let rec recurse () =
        async {
            let! connected = store.CheckConnectionAsync()
            if connected then
                Log.Information("connected to Mongo!")
                healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.Serving)
                do! Async.Sleep(60 * 1000)
                do! recurse ()
            else
                Log.Information("could not connect to Mongo!")
                healthService.SetStatus("", HealthCheckResponse.Types.ServingStatus.NotServing)
                do! Async.Sleep(60 * 1000)
                do! recurse ()
        }
    recurse ()

[<EntryPoint>]
let main argv =
    let appName = "Vehicle Reader"
    let debug = Env.getEnv "DEBUG" "false" |> bool.Parse
    let seqConfig = SeqConfig.Load()
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
    Log.Debug("{@Config}", config)
    let store = Store(config.MongoConfig)
    let vehicleQueryService = VehicleQueryServiceImpl(store)
    let healthService = HealthServiceImpl()
    let server = Server()
    server.Services.Add(CosmicDealership.Vehicle.V1.VehicleQueryService.BindService(vehicleQueryService))
    server.Services.Add(Health.BindService(healthService))
    let serverPort = ServerPort(config.ServerConfig.Host, config.ServerConfig.Port, ServerCredentials.Insecure)
    let listenPort = server.Ports.Add(serverPort)
    let healthCheck = checkHealth store healthService
    use cancellation = new CancellationTokenSource()
    server.Start()
    Async.Start(healthCheck, cancellation.Token)
    Log.Information("🚀 Server listening at :{Port}", listenPort)
    Log.Information("🍃 Connected to MongoDB at {Url}", config.MongoConfig.Url)
    Log.Information("📜 Logs sent to {Url}", seqConfig.Url)
    let exitEvent = new AutoResetEvent(false)
    Console.CancelKeyPress |> Event.add (fun _ ->
        Log.Information("shutting down...")
        cancellation.Cancel()
        exitEvent.Set() |> ignore)
    exitEvent.WaitOne() |> ignore
    server.ShutdownAsync().Wait()
    Log.CloseAndFlush()
    0 // return an integer exit code
