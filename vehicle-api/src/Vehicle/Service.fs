namespace Vehicle

open Grpc.Core

module Dto =

    module Vehicle =
        let fromProto (proto:Tutorial.Vehicle.V1.Vehicle) =
            let vehicleId = VehicleId.fromString proto.VehicleId
            { VehicleId = vehicleId
              Make = proto.Make
              Model = proto.Model
              Year = proto.Year }
        let toProto (vehicle:Vehicle) =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId=(VehicleId.toString vehicle.VehicleId),
                Make=vehicle.Make,
                Model=vehicle.Model,
                Year=vehicle.Year)

    module VehicleStatus =
        let toProto (vehicleStatus:VehicleStatus) =
            match vehicleStatus with
            | Unknown -> Tutorial.Vehicle.V1.VehicleStatus.Unspecified
            | Available -> Tutorial.Vehicle.V1.VehicleStatus.Available
            | Removed -> Tutorial.Vehicle.V1.VehicleStatus.Removed
            | Leased -> Tutorial.Vehicle.V1.VehicleStatus.Leased
            

    module VehicleState =
        let toProto (vehicleState:VehicleState) =
            Tutorial.Vehicle.V1.VehicleState(
                Vehicle=(vehicleState.Vehicle |> Option.map Vehicle.toProto |> Option.defaultValue null),
                VehicleStatus=(vehicleState.VehicleStatus |> VehicleStatus.toProto))

type VehicleServiceImpl(store:Store) =
    inherit Tutorial.Vehicle.V1.VehicleService.VehicleServiceBase()

    override _.ListVehicles(req:Tutorial.Vehicle.V1.ListVehiclesRequest, context:ServerCallContext) =
        async {
            // ref: https://eventstore.com/docs/projections/system-projections/index.html?tabs=tabid-5#by-event-type
            let streamName = "$et-VehicleAdded"
            let pageToken = PageToken.fromString req.PageToken
            let pageSize = PageSize.fromInt req.PageSize
            let streamStart = PageToken.decode pageToken
            let! streamSlice = store.ReadStream(streamName, pageToken, pageSize)
            let totalCount = streamSlice.LastEventNumber + 1L
            let prevPageToken =
                match streamStart - (req.PageSize |> int64) with
                | c when c <= 0L -> PageToken.fromString ""
                | c -> c |> PageToken.encode
            let nextPageToken = 
                if streamSlice.IsEndOfStream then PageToken.fromString ""
                else PageToken.encode streamSlice.NextEventNumber
            let tryGetVehicle (event:VehicleEvent) =
                match event with
                | VehicleAdded vehicle -> Some vehicle
                | _ -> None
            let vehicles =
                streamSlice.Events
                |> Seq.choose (store.TryDecodeEvent >> Option.bind tryGetVehicle)
                |> Seq.map Dto.Vehicle.toProto
            let res =
                Tutorial.Vehicle.V1.ListVehiclesResponse(
                    TotalCount=totalCount,
                    PrevPageToken=(PageToken.toString prevPageToken),
                    NextPageToken=(PageToken.toString nextPageToken))
            res.Vehicles.AddRange(vehicles)
            return res
        } |> Async.StartAsTask

    override _.GetVehicle(req:Tutorial.Vehicle.V1.GetVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = VehicleId.fromString req.VehicleId
            let stream = store.ResolveVehicle(vehicleId)
            let! vehicleState = stream.Query(id)
            let res =
                Tutorial.Vehicle.V1.GetVehicleResponse(
                    Vehicle=(vehicleState |> Dto.VehicleState.toProto))
            return res
        } |> Async.StartAsTask

    override _.AddVehicle(req:Tutorial.Vehicle.V1.AddVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = VehicleId.fromString req.Vehicle.VehicleId
            let vehicle = Dto.Vehicle.fromProto req.Vehicle
            let stream = store.ResolveVehicle(vehicleId)
            let addVehicle = AddVehicle vehicle
            do! stream.Transact(Aggregate.interpret vehicleId addVehicle)
            let msg = sprintf "successfully added Vehicle-%s" req.Vehicle.VehicleId
            return Tutorial.Vehicle.V1.AddVehicleResponse(Message=msg)
        } |> Async.StartAsTask

    override _.RemoveVehicle(req:Tutorial.Vehicle.V1.RemoveVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = VehicleId.fromString req.VehicleId
            let stream = store.ResolveVehicle(vehicleId)
            let removeVehicle = RemoveVehicle
            do! stream.Transact(Aggregate.interpret vehicleId removeVehicle)
            let msg = sprintf "successfully removed Vehicle-%s" req.VehicleId
            return Tutorial.Vehicle.V1.RemoveVehicleResponse(Message=msg)
        } |> Async.StartAsTask

    override _.LeaseVehicle(req:Tutorial.Vehicle.V1.LeaseVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = VehicleId.fromString req.VehicleId
            let stream = store.ResolveVehicle(vehicleId)
            let leaseVehicle = LeaseVehicle
            do! stream.Transact(Aggregate.interpret vehicleId leaseVehicle)
            let msg = sprintf "successfully leased Vehicle-%s" req.VehicleId
            return Tutorial.Vehicle.V1.LeaseVehicleResponse(Message=msg)
        } |> Async.StartAsTask

    override _.ReturnVehicle(req:Tutorial.Vehicle.V1.ReturnVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = VehicleId.fromString req.VehicleId
            let stream = store.ResolveVehicle(vehicleId)
            let returnVehicle = ReturnVehicle
            do! stream.Transact(Aggregate.interpret vehicleId returnVehicle)
            let msg = sprintf "successfully returned Vehicle-%s" req.VehicleId
            return Tutorial.Vehicle.V1.ReturnVehicleResponse(Message=msg)
        } |> Async.StartAsTask
