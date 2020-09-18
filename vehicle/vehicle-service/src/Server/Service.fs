namespace Server

open Grpc.Core
open Serilog
open Shared
open System

type VehicleServiceImpl(store:Store) =
    inherit CosmicDealership.Vehicle.V1.VehicleService.VehicleServiceBase()

    let authorize (user:CosmicDealership.User.V1.User) (permission:string) =
        if (user.Permissions |> Seq.contains permission) then
            Ok "authorized"
        else
            Error (sprintf "user %s does not have %s permission" user.UserId permission)

    override _.AddVehicle(req:CosmicDealership.Vehicle.V1.AddVehicleRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "add:vehicles" with
                | Ok _ ->
                    match VehicleId.fromString req.VehicleId with
                    | Ok vehicleId ->
                        match Dto.Vehicle.fromProto req.Vehicle with
                        | Ok vehicle ->
                            let stream = store.ResolveVehicle(vehicleId)
                            let addVehicle = AddVehicle vehicle
                            match! stream.Transact(Aggregate.decide vehicleId addVehicle) with
                            | Ok msg ->
                                return CosmicDealership.Vehicle.V1.AddVehicleResponse(Success=msg)
                            | Error (VehicleAlreadyExists msg) ->
                                return CosmicDealership.Vehicle.V1.AddVehicleResponse(VehicleAlreadyExists=msg)
                            | Error other ->
                                return failwithf "Unhandled error %A" other
                        | Error validationError ->
                            return CosmicDealership.Vehicle.V1.AddVehicleResponse(VehicleInvalid=validationError)
                    | Error vehicleIdError ->
                        return CosmicDealership.Vehicle.V1.AddVehicleResponse(VehicleInvalid=vehicleIdError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.AddVehicleResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.UpdateVehicle(req:CosmicDealership.Vehicle.V1.UpdateVehicleRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "update:vehicles" with
                | Ok _ ->
                    match VehicleId.fromString req.VehicleId with
                    | Ok vehicleId ->
                        match Dto.Vehicle.fromProto req.Vehicle with
                        | Ok vehicle ->
                            let stream = store.ResolveVehicle(vehicleId)
                            let updateVehicle = UpdateVehicle vehicle
                            match! stream.Transact(Aggregate.decide vehicleId updateVehicle) with
                            | Ok msg ->
                                return CosmicDealership.Vehicle.V1.UpdateVehicleResponse(Success=msg)
                            | Error (VehicleNotFound msg) ->
                                return CosmicDealership.Vehicle.V1.UpdateVehicleResponse(VehicleNotFound=msg)
                            | Error other ->
                                return failwithf "Unhandled error %A" other
                        | Error validationError ->
                            return CosmicDealership.Vehicle.V1.UpdateVehicleResponse(VehicleInvalid=validationError)
                    | Error vehicleIdError ->
                        return CosmicDealership.Vehicle.V1.UpdateVehicleResponse(VehicleNotFound=vehicleIdError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.UpdateVehicleResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.AddImage(req:CosmicDealership.Vehicle.V1.AddImageRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "update:vehicles" with
                | Ok _ ->
                    match VehicleId.fromString req.VehicleId with 
                    | Ok vehicleId ->
                        let stream = store.ResolveVehicle(vehicleId)
                        let imageUri = Uri(req.ImageUri)
                        let addVehicleImage = AddImage imageUri
                        match! stream.Transact(Aggregate.decide vehicleId addVehicleImage) with
                        | Ok msg ->
                            return CosmicDealership.Vehicle.V1.AddImageResponse(Success=msg)
                        | Error (VehicleNotFound msg) ->
                            return CosmicDealership.Vehicle.V1.AddImageResponse(VehicleNotFound=msg)
                        | Error other ->
                            return failwithf "Unhandled erorr %A" other
                    | Error vehicleIdError ->
                        return CosmicDealership.Vehicle.V1.AddImageResponse(VehicleNotFound=vehicleIdError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.AddImageResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error(ex, "Error adding image to Vehicle-{VehicleId}", req.VehicleId)
                return raise ex
        } |> Async.StartAsTask

    override _.RemoveVehicle(req:CosmicDealership.Vehicle.V1.RemoveVehicleRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "remove:vehicles" with
                | Ok _ ->
                    match VehicleId.fromString req.VehicleId with
                    | Ok vehicleId ->
                        let stream = store.ResolveVehicle(vehicleId)
                        let removeVehicle = RemoveVehicle
                        match! stream.Transact(Aggregate.decide vehicleId removeVehicle) with
                        | Ok msg ->
                            return CosmicDealership.Vehicle.V1.RemoveVehicleResponse(Success=msg)
                        | Error (VehicleNotFound msg) ->
                            return CosmicDealership.Vehicle.V1.RemoveVehicleResponse(VehicleNotFound=msg)
                        | Error (VehicleCurrentlyLeased msg) ->
                            return CosmicDealership.Vehicle.V1.RemoveVehicleResponse(VehicleCurrentlyLeased=msg)
                        | Error other ->
                            return failwithf "Unhandled error %A" other
                    | Error vehicleIdError ->
                        return CosmicDealership.Vehicle.V1.RemoveVehicleResponse(VehicleNotFound=vehicleIdError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.RemoveVehicleResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.LeaseVehicle(req:CosmicDealership.Vehicle.V1.LeaseVehicleRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "lease:vehicles" with
                | Ok _ ->
                    match VehicleId.fromString req.VehicleId with
                    | Ok vehicleId ->
                        let stream = store.ResolveVehicle(vehicleId)
                        let leaseVehicle = LeaseVehicle
                        match! stream.Transact(Aggregate.decide vehicleId leaseVehicle) with
                        | Ok msg ->
                            return CosmicDealership.Vehicle.V1.LeaseVehicleResponse(Success=msg)
                        | Error (VehicleNotFound msg) ->
                            return CosmicDealership.Vehicle.V1.LeaseVehicleResponse(VehicleNotFound=msg)
                        | Error (VehicleCurrentlyLeased msg) ->
                            return CosmicDealership.Vehicle.V1.LeaseVehicleResponse(VehicleCurrentlyLeased=msg)
                        | Error other ->
                            return failwithf "Unhandled error %A" other
                    | Error vehicleIdError ->
                        return CosmicDealership.Vehicle.V1.LeaseVehicleResponse(VehicleNotFound=vehicleIdError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.LeaseVehicleResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.ReturnVehicle(req:CosmicDealership.Vehicle.V1.ReturnVehicleRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "return:vehicles" with
                | Ok _ ->
                    match VehicleId.fromString req.VehicleId with
                    | Ok vehicleId ->
                        let stream = store.ResolveVehicle(vehicleId)
                        let returnVehicle = ReturnVehicle
                        match! stream.Transact(Aggregate.decide vehicleId returnVehicle) with
                        | Ok msg ->
                            return CosmicDealership.Vehicle.V1.ReturnVehicleResponse(Success=msg)
                        | Error (VehicleNotFound msg) ->
                            return CosmicDealership.Vehicle.V1.ReturnVehicleResponse(VehicleNotFound=msg)
                        | Error (VehicleAlreadyReturned msg) ->
                            return CosmicDealership.Vehicle.V1.ReturnVehicleResponse(VehicleAlreadyReturned=msg)
                        | Error other ->
                            return failwithf "Unhandled error %A" other
                    | Error vehicleIdError ->
                        return CosmicDealership.Vehicle.V1.ReturnVehicleResponse(VehicleNotFound=vehicleIdError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.ReturnVehicleResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask
