namespace Server

open Grpc.Core
open Serilog
open Shared

type VehicleServiceImpl(store:Store) =
    inherit CosmicDealership.Vehicle.V1.VehicleService.VehicleServiceBase()

    let authorize (user:CosmicDealership.User.V1.User) (permission:string) =
        if not (user.Permissions |> Seq.contains permission) then
            sprintf "user %s does not have %s permission" user.UserId permission
            |> RpcException.raisePermissionDenied

    override _.AddVehicle(req:CosmicDealership.Vehicle.V1.AddVehicleRequest, context:ServerCallContext) =
        async {
            try
                authorize req.User "add:vehicles"
                let vehicleId = VehicleId.fromString req.Vehicle.VehicleId
                let vehicle = Dto.Vehicle.fromProto req.Vehicle
                let stream = store.ResolveVehicle(vehicleId)
                let addVehicle = AddVehicle vehicle
                do! stream.Transact(Aggregate.interpret vehicleId addVehicle)
                let msg = sprintf "successfully added Vehicle-%s" req.Vehicle.VehicleId
                return CosmicDealership.Vehicle.V1.AddVehicleResponse(Message=msg)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.UpdateVehicle(req:CosmicDealership.Vehicle.V1.UpdateVehicleRequest, context:ServerCallContext) =
        async {
            try
                authorize req.User "update:vehicles"
                let vehicleId = VehicleId.fromString req.VehicleId
                let updates = Dto.VehicleUpdates.fromProto req.Updates
                let stream = store.ResolveVehicle(vehicleId)
                let updateVehicle = UpdateVehicle updates
                do! stream.Transact(Aggregate.interpret vehicleId updateVehicle)
                let msg = sprintf "successfully updated Vehicle-%s" req.VehicleId
                return CosmicDealership.Vehicle.V1.UpdateVehicleResponse(Message=msg)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.RemoveVehicle(req:CosmicDealership.Vehicle.V1.RemoveVehicleRequest, context:ServerCallContext) =
        async {
            try
                authorize req.User "remove:vehicles"
                let vehicleId = VehicleId.fromString req.VehicleId
                let stream = store.ResolveVehicle(vehicleId)
                let removeVehicle = RemoveVehicle
                do! stream.Transact(Aggregate.interpret vehicleId removeVehicle)
                let msg = sprintf "successfully removed Vehicle-%s" req.VehicleId
                return CosmicDealership.Vehicle.V1.RemoveVehicleResponse(Message=msg)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.LeaseVehicle(req:CosmicDealership.Vehicle.V1.LeaseVehicleRequest, context:ServerCallContext) =
        async {
            try
                authorize req.User "lease:vehicles"
                let vehicleId = VehicleId.fromString req.VehicleId
                let stream = store.ResolveVehicle(vehicleId)
                let leaseVehicle = LeaseVehicle
                do! stream.Transact(Aggregate.interpret vehicleId leaseVehicle)
                let msg = sprintf "successfully leased Vehicle-%s" req.VehicleId
                return CosmicDealership.Vehicle.V1.LeaseVehicleResponse(Message=msg)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.ReturnVehicle(req:CosmicDealership.Vehicle.V1.ReturnVehicleRequest, context:ServerCallContext) =
        async {
            try
                authorize req.User "return:vehicles"
                let vehicleId = VehicleId.fromString req.VehicleId
                let stream = store.ResolveVehicle(vehicleId)
                let returnVehicle = ReturnVehicle
                do! stream.Transact(Aggregate.interpret vehicleId returnVehicle)
                let msg = sprintf "successfully returned Vehicle-%s" req.VehicleId
                return CosmicDealership.Vehicle.V1.ReturnVehicleResponse(Message=msg)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask
