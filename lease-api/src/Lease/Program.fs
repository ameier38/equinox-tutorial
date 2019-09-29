open Grpc.Core
open Grpc.HealthCheck
open Grpc.Health.V1
open Lease
open Lease.Projection
open Lease.Store
open Lease.Service
open Serilog
open System
open System.Threading

[<EntryPoint>]
let main _ =
    let getUtcNow () = DateTime.UtcNow
    let config = Config.Load()
    let logger = 
        LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Seq(config.Seq.Url)
            .CreateLogger()
    let store = Store(config)
    let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
    let codec = FsCodec.NewtonsoftJson.Codec.Create<StoredEvent>(serializationSettings)
    let leaseAPI = LeaseAPIImpl(getUtcNow, store, codec, logger)
    let healthService = HealthServiceImpl()
    let projectionManager = ProjectionManager(config, logger)
    let server = Server()

    healthService.SetStatus("lease", HealthCheckResponse.Types.ServingStatus.Serving)
    server.Services.Add(Tutorial.Lease.V1.LeaseAPI.BindService(leaseAPI))
    server.Services.Add(Health.BindService(healthService))
    server.Ports.Add(ServerPort(config.Server.Host, config.Server.Port, ServerCredentials.Insecure)) |> ignore
    server.Start()

    projectionManager.StartProjections()

    logger.Information(sprintf "logging at %s 📝" config.Seq.Url)
    logger.Information(sprintf "using Event Store at %A" config.EventStore.DiscoveryUri)
    logger.Information(sprintf "serving at %s:%d 🚀" config.Server.Host config.Server.Port)
    let exitEvent = new AutoResetEvent(false)
    let exit = new ConsoleCancelEventHandler(fun _ _ ->
        exitEvent.Set() |> ignore)
    Console.CancelKeyPress.AddHandler exit
    exitEvent.WaitOne() |> ignore
    server.ShutdownAsync().Wait()
    Log.CloseAndFlush()
    0 // return an integer exit code
