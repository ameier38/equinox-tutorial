namespace Reactor

open EventStore.ClientAPI
open Shared
open Serilog
open System
open System.Text
open System.Threading.Tasks

type Subscription(eventstoreConfig:EventStoreConfig, store:Store) =
    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()
    let decode (streamEvent:StreamEvent) =
        let checkpoint = streamEvent.Event.Index
        let vehicleEventOpt = codec.TryDecode streamEvent.Event
        checkpoint, vehicleEventOpt
    let tryDecodeEvent = UnionEncoderAdapters.encodedEventOfResolvedEvent >> decode

    let eventstore = EventStoreConnection.Create(Uri(eventstoreConfig.Url))
    do eventstore.ConnectAsync().Wait()

    let onVehicleEvent (checkpoint:int64, vehicleEventOpt:VehicleEvent option) =
        async {
            match vehicleEventOpt with
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
                    do! store.AddVehicle(checkpoint, vehicleDto)
                | VehicleRemoved payload ->
                    let vehicleId = VehicleId.toString payload.VehicleId
                    let newStatus = VehicleStatus.toString Removed
                    do! store.SetVehicleStatus(checkpoint, vehicleId, newStatus)
                | VehicleLeased payload ->
                    let vehicleId = VehicleId.toString payload.VehicleId
                    let newStatus = VehicleStatus.toString Leased
                    do! store.SetVehicleStatus(checkpoint, vehicleId, newStatus)
                | VehicleReturned payload ->
                    let vehicleId = VehicleId.toString payload.VehicleId
                    let newStatus = VehicleStatus.toString Available
                    do! store.SetVehicleStatus(checkpoint, vehicleId, newStatus)
            | None ->
                Log.Information("not a vehicle event; skipping")
        } |> Async.StartAsTask

    member _.Start() =
        async {
            let stream = "$ce-Vehicle"
            let settings =
                CatchUpSubscriptionSettings(
                    maxLiveQueueSize = 10,
                    readBatchSize = 10,
                    verboseLogging = false,
                    resolveLinkTos = true,
                    subscriptionName = "vehicles")
            let credentials =
                SystemData.UserCredentials(
                    username = eventstoreConfig.User,
                    password = eventstoreConfig.Password)
            let! checkpoint = store.GetVehicleCheckpoint()
            let nullableCheckpoint = new Nullable<int64>(checkpoint)
            let eventAppeared =
                Func<EventStoreCatchUpSubscription,ResolvedEvent,Task>(fun _ re -> 
                    Log.Information("received event {@ResolvedEvent}", re)
                    tryDecodeEvent re |> onVehicleEvent :> Task)
            let subscriptionDropped =
                Action<EventStoreCatchUpSubscription,SubscriptionDropReason,exn>(fun _ r e ->
                    Log.Information(e, "subscription dropped {Reason}", r))
            Log.Information("subscribing to {Stream} from checkpoint {Checkpoint}", stream, checkpoint)
            // ref: https://eventstore.com/docs/projections/system-projections/index.html?tabs=tabid-5#by-category
            let subscription =
                eventstore.SubscribeToStreamFrom(
                    stream = "$ce-Vehicle",
                    lastCheckpoint = nullableCheckpoint,
                    settings = settings,
                    eventAppeared = eventAppeared,
                    subscriptionDropped = subscriptionDropped,
                    userCredentials = credentials)
            Console.ReadKey() |> ignore
            Log.Information("subscription ended {@Subscription}", subscription)
        }
    