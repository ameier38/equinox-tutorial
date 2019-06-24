open Grpc.Core
open Grpc.HealthCheck
open Grpc.Health.V1
open Lease
open Lease.Store
open Lease.Service
open Serilog
open System
open System.Threading

[<EntryPoint>]
let main _ =
    let logger = LoggerConfiguration().WriteTo.Console().CreateLogger()
    let config = Config.load()
    let store = Store(config)
    let serializationSettings = Newtonsoft.Json.JsonSerializerSettings()
    let codec = Equinox.Codec.NewtonsoftJson.Json.Create<StoredEvent>(serializationSettings)
    let leaseService = LeaseServiceImpl(store, logger)
    let healthService = HealthServiceImpl()
    let server = Server()
    let host = "0.0.0.0"
    healthService.SetStatus("lease", HealthCheckResponse.Types.ServingStatus.Serving)
    server.Services.Add(Proto.Lease.LeaseService.BindService(leaseService))
    server.Services.Add(Health.BindService(healthService))
    server.Ports.Add(ServerPort(host, config.Port, ServerCredentials.Insecure)) |> ignore
    server.Start()

    printfn "serving at %s:%d" host config.Port
    let exitEvent = new AutoResetEvent(false)
    let exit = new ConsoleCancelEventHandler(fun _ _ ->
        exitEvent.Set() |> ignore)
    Console.CancelKeyPress.AddHandler exit
    exitEvent.WaitOne() |> ignore
    server.ShutdownAsync().Wait()
    0 // return an integer exit code
