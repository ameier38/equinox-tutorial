module Vehicle.Service

open Grpc.Core
open Tutorial.Vehicle.V1

module Vehicle =
    let fromProto (proto:Vehicle) =
        { Make = proto.Make
          Model = proto.Model
          Year = proto.Year }

type Service(store:Store.IStore) =
    inherit VehicleService.VehicleServiceBase()

    override _.AddVehicle(req:AddVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = Guid.parse<vehicleId> req.VehicleId
            let vehicle = Vehicle.fromProto req.Vehicle
            let stream = store.Resolve(vehicleId)
            let addVehicle = AddVehicle vehicle
            do! stream.Transact(Aggregate.interpret vehicleId addVehicle)
            let msg = sprintf "successfully added Vehicle-%s" req.VehicleId
            return AddVehicleResponse(Message=msg)
        } |> Async.StartAsTask

    override _.RemoveVehicle(req:RemoveVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = Guid.parse<vehicleId> req.VehicleId
            let stream = store.Resolve(vehicleId)
            let removeVehicle = RemoveVehicle
            do! stream.Transact(Aggregate.interpret vehicleId removeVehicle)
            let msg = sprintf "successfully removed Vehicle-%s" req.VehicleId
            return RemoveVehicleResponse(Message=msg)
        } |> Async.StartAsTask

    override _.LeaseVehicle(req:LeaseVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = Guid.parse<vehicleId> req.VehicleId
            let stream = store.Resolve(vehicleId)
            let leaseVehicle = LeaseVehicle
            do! stream.Transact(Aggregate.interpret vehicleId leaseVehicle)
            let msg = sprintf "successfully leased Vehicle-%s" req.VehicleId
            return LeaseVehicleResponse(Message=msg)
        } |> Async.StartAsTask

    override _.ReturnVehicle(req:ReturnVehicleRequest, context:ServerCallContext) =
        async {
            let vehicleId = Guid.parse<vehicleId> req.VehicleId
            let stream = store.Resolve(vehicleId)
            let returnVehicle = ReturnVehicle
            do! stream.Transact(Aggregate.interpret vehicleId returnVehicle)
            let msg = sprintf "successfully returned Vehicle-%s" req.VehicleId
            return ReturnVehicleResponse(Message=msg)
        } |> Async.StartAsTask
