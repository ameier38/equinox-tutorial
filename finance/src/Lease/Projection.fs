module Lease.Projection

open EventStore.ClientAPI.Projections
open EventStore.ClientAPI.SystemData
open Serilog
open System
open System.IO
open System.Net

// ref: https://github.com/jet/equinox/blob/master/src/Equinox.EventStore/EventStore.fs#L496
type SerilogAdapter(log : ILogger) =
    interface EventStore.ClientAPI.ILogger with
        member __.Debug(format: string, args: obj []) =           log.Debug(format, args)
        member __.Debug(ex: exn, format: string, args: obj []) =  log.Debug(ex, format, args)
        member __.Info(format: string, args: obj []) =            log.Information(format, args)
        member __.Info(ex: exn, format: string, args: obj []) =   log.Information(ex, format, args)
        member __.Error(format: string, args: obj []) =           log.Error(format, args)
        member __.Error(ex: exn, format: string, args: obj []) =  log.Error(ex, format, args)

type ProjectionManager(config:Config, logger: Core.Logger) =
    let log = SerilogAdapter(logger)
    let { Host = host; HttpPort = port; User = user; Password = password } = config.EventStore
    let endpoint = DnsEndPoint(host, port)
    let creds = UserCredentials(user, password)
    let projectionsDir = Path.Combine(AppContext.BaseDirectory,  "projections")
    let projectionManager = ProjectionsManager(log, endpoint, TimeSpan.FromSeconds(5.0))
    let projections = [ "LeaseCreated" ]

    let rec retry work = async {
        try
            let! res = work
            return res
        with ex ->
            logger.Warning(ex, "work failed; retrying...")
            do! Async.Sleep(10000)
            return! retry work
        }

    let listProjections () =
        async {
            let! projections = 
                projectionManager.ListAllAsync(creds)
                |> Async.AwaitTask
            return projections |> Seq.map (fun pd -> pd.Name) |> Seq.toList
        }

    let createProjection projectionName projectionCode =
        logger.Information(sprintf "creating projection: %s" projectionName)
        projectionManager.CreateContinuousAsync(projectionName, projectionCode, true, creds)
        |> Async.AwaitTask

    let startProjection projectionName =
        let projectionPath = Path.Combine(projectionsDir, projectionName + ".js")
        let projectionCode = File.ReadAllText(projectionPath)
        async {
            let! projections = listProjections()
            logger.Information(sprintf "existing projections: %A" projections)
            if not (projections |> List.contains projectionName) then
                do! createProjection projectionName projectionCode
            let! projections = listProjections()
            logger.Information(sprintf "new projections: %A" projections)
        }

    member __.StartProjections() =
        logger.Information(sprintf "starting projections on %s:%d 👓" host port)
        projections
        |> List.map (startProjection >> retry)
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously