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
    let config = Config.load()
    let logger = 
        LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Seq(config.Seq.Url)
            .CreateLogger()
    let store = Store(config)
    let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
    let codec = Equinox.Codec.NewtonsoftJson.Json.Create<StoredEvent>(serializationSettings)
    let leaseResolver = StreamResolver(store, codec, "lease", Aggregate.foldLeaseStream, Aggregate.initialLeaseStream)
    let leaseEventListResolver = StreamResolver(store, codec, "events", Aggregate.foldLeaseEventList, [])
    let leaseListResolver = StreamResolver(store, codec, "leases", Aggregate.foldLeaseList, [])
    let leaseAPI = LeaseAPIImpl(getUtcNow, leaseResolver, leaseEventListResolver, leaseListResolver, logger)
    let healthService = HealthServiceImpl()
    let projectionManager = ProjectionManager(config, logger)
    let server = Server()
    let host = "0.0.0.0"

    healthService.SetStatus("lease", HealthCheckResponse.Types.ServingStatus.Serving)
    server.Services.Add(Tutorial.Lease.V1.LeaseAPI.BindService(leaseAPI))
    server.Services.Add(Health.BindService(healthService))
    server.Ports.Add(ServerPort(host, config.Port, ServerCredentials.Insecure)) |> ignore
    server.Start()

    projectionManager.StartProjections()

    logger.Information(sprintf "logging at %s 📝" config.Seq.Url)
    logger.Information(sprintf "serving at %s:%d 🚀" host config.Port)
    let exitEvent = new AutoResetEvent(false)
    let exit = new ConsoleCancelEventHandler(fun _ _ ->
        exitEvent.Set() |> ignore)
    Console.CancelKeyPress.AddHandler exit
    exitEvent.WaitOne() |> ignore
    server.ShutdownAsync().Wait()
    Log.CloseAndFlush()
    0 // return an integer exit code
