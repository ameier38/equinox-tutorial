module Reactor.ReadModel

open FSharp.Control
open FSharp.UMX
open MongoDB.Driver
open Shared
open Serilog
open System.Threading

type VehicleDto =
    { vehicleId: string
      make: string
      model: string
      year: int
      status: string }

module VehicleStatus =
    let toString =
        function
        | Unknown -> "Unknown"
        | Available -> "Available"
        | Removed -> "Removed"
        | Leased -> "Leased"

type VehicleReadModel(config:Config) =
    let name = "vehicles"
    let stream = UMX.tag<stream> "$ce-Vehicle"
    let store = Store(config.MongoConfig)
    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()
    let vehicleCollection = store.GetCollection(name)

    let addVehicle (vehicle:VehicleDto) =
        async {
            Log.Debug("adding vehicle {Vehicle}", vehicle)
            // use replace with upsert to make idempotent
            let filterVehicle = Builders<VehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicle.vehicleId)
            let replaceOptions = ReplaceOptions(IsUpsert = true)
            do! vehicleCollection.ReplaceOneAsync(filterVehicle, vehicle, replaceOptions)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let setVehicleStatus (vehicleId:string) (status:string) =
        async {
            Log.Debug("updating Vehicle-{VehicleId} status to {VehicleStatus}", status)
            let filterVehicle = Builders<VehicleDto>.Filter.Where(fun v -> v.vehicleId = vehicleId)
            let updateStatus = Builders<VehicleDto>.Update.Set((fun v -> v.status), status)
            do! vehicleCollection.UpdateOneAsync(filterVehicle, updateStatus)
                |> Async.AwaitTask
                |> Async.Ignore
        }

    let handleEvent: EventHandler =
        fun event ->
            async {
                let decodedEvent = codec.TryDecode event
                match decodedEvent with
                | Some vehicleEvent ->
                    match vehicleEvent with
                    | VehicleAdded vehicle ->
                        let vehicleId = VehicleId.toString vehicle.VehicleId
                        let vehicleDto =
                            { vehicleId = vehicleId
                              make = vehicle.Make
                              model = vehicle.Model
                              year = vehicle.Year
                              status = VehicleStatus.toString Available }
                        do! addVehicle vehicleDto
                    | VehicleRemoved payload ->
                        let vehicleId = VehicleId.toString payload.VehicleId
                        let newStatus = VehicleStatus.toString Removed
                        do! setVehicleStatus vehicleId newStatus
                    | VehicleLeased payload ->
                        let vehicleId = VehicleId.toString payload.VehicleId
                        let newStatus = VehicleStatus.toString Leased
                        do! setVehicleStatus vehicleId newStatus
                    | VehicleReturned payload ->
                        let vehicleId = VehicleId.toString payload.VehicleId
                        let newStatus = VehicleStatus.toString Available
                        do! setVehicleStatus vehicleId newStatus
                | None -> ()
                do! store.UpdateCheckpoint(name, event.Index)
            }

    let subscription = Subscription(name, stream, handleEvent, config.EventStoreConfig)

    member _.StartAsync(cancellationToken:CancellationToken) =
        async {
            // ref: https://eventstore.com/docs/projections/system-projections/index.html?tabs=tabid-5#by-category
            let! rawCheckpoint = store.GetCheckpoint(name)
            let checkpoint = UMX.tag<checkpoint> rawCheckpoint
            do! subscription.SubscribeAsync(checkpoint, cancellationToken)
        }
