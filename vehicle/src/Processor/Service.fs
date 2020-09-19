namespace Server

open Grpc.Core
open Serilog
open Shared

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
                    let vehicleId = VehicleId.parse req.VehicleId
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
                    let vehicleId = VehicleId.parse req.VehicleId
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
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.UpdateVehicleResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask

    override _.UpdateAvatar(req:CosmicDealership.Vehicle.V1.UpdateAvatarRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "update:vehicles" with
                | Ok _ ->
                    let vehicleId = VehicleId.parse req.VehicleId
                    let stream = store.ResolveVehicle(vehicleId)
                    match Dto.Url.validate req.AvatarUrl with
                    | Ok avatarUrl ->
                        let updateAvatar = UpdateAvatar avatarUrl
                        match! stream.Transact(Aggregate.decide vehicleId updateAvatar) with
                        | Ok msg ->
                            return CosmicDealership.Vehicle.V1.UpdateAvatarResponse(Success=msg)
                        | Error (VehicleNotFound msg) ->
                            return CosmicDealership.Vehicle.V1.UpdateAvatarResponse(VehicleNotFound=msg)
                        | Error other ->
                            return failwithf "Unhandled erorr %A" other
                    | Error validationError ->
                        return CosmicDealership.Vehicle.V1.UpdateAvatarResponse(AvatarInvalid=validationError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.UpdateAvatarResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error(ex, "Error updating Vehicle-{VehicleId} avatar", req.VehicleId)
                return raise ex
        } |> Async.StartAsTask

    override _.RemoveAvatar(req:CosmicDealership.Vehicle.V1.RemoveAvatarRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "update:vehicles" with
                | Ok _ ->
                    let vehicleId = VehicleId.parse req.VehicleId
                    let stream = store.ResolveVehicle(vehicleId)
                    match! stream.Transact(Aggregate.decide vehicleId RemoveAvatar) with
                    | Ok msg ->
                        return CosmicDealership.Vehicle.V1.RemoveAvatarResponse(Success=msg)
                    | Error (VehicleNotFound msg) ->
                        return CosmicDealership.Vehicle.V1.RemoveAvatarResponse(VehicleNotFound=msg)
                    | Error other ->
                        return failwithf "Unhandled erorr %A" other
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.RemoveAvatarResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error(ex, "Error removing Vehicle-{VehicleId} avatar", req.VehicleId)
                return raise ex
        } |> Async.StartAsTask

    override _.AddImage(req:CosmicDealership.Vehicle.V1.AddImageRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "update:vehicles" with
                | Ok _ ->
                    let vehicleId = VehicleId.parse req.VehicleId
                    let stream = store.ResolveVehicle(vehicleId)
                    match Dto.Url.validate req.ImageUrl with
                    | Ok imageUrl ->
                        let addImage = AddImage imageUrl
                        match! stream.Transact(Aggregate.decide vehicleId addImage) with
                        | Ok msg ->
                            return CosmicDealership.Vehicle.V1.AddImageResponse(Success=msg)
                        | Error (VehicleNotFound msg) ->
                            return CosmicDealership.Vehicle.V1.AddImageResponse(VehicleNotFound=msg)
                        | Error (MaxImageCountReached msg) ->
                            return CosmicDealership.Vehicle.V1.AddImageResponse(MaxImageCountReached=msg)
                        | Error other ->
                            return failwithf "Unhandled erorr %A" other
                    | Error validationError ->
                        return CosmicDealership.Vehicle.V1.AddImageResponse(ImageInvalid=validationError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.AddImageResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error(ex, "Error adding image to Vehicle-{VehicleId}", req.VehicleId)
                return raise ex
        } |> Async.StartAsTask

    override _.RemoveImage(req:CosmicDealership.Vehicle.V1.RemoveImageRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "update:vehicles" with
                | Ok _ ->
                    let vehicleId = VehicleId.parse req.VehicleId
                    let stream = store.ResolveVehicle(vehicleId)
                    match Dto.Url.validate req.ImageUrl with
                    | Ok imageUrl ->
                        let removeImage = RemoveImage imageUrl
                        match! stream.Transact(Aggregate.decide vehicleId removeImage) with
                        | Ok msg ->
                            return CosmicDealership.Vehicle.V1.RemoveImageResponse(Success=msg)
                        | Error (VehicleNotFound msg) ->
                            return CosmicDealership.Vehicle.V1.RemoveImageResponse(VehicleNotFound=msg)
                        | Error (ImageNotFound msg) ->
                            return CosmicDealership.Vehicle.V1.RemoveImageResponse(ImageNotFound=msg)
                        | Error other ->
                            return failwithf "Unhandled erorr %A" other
                    | Error validationError ->
                        return CosmicDealership.Vehicle.V1.RemoveImageResponse(ImageInvalid=validationError)
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.RemoveImageResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error(ex, "Error adding image to Vehicle-{VehicleId}", req.VehicleId)
                return raise ex
        } |> Async.StartAsTask

    override _.RemoveVehicle(req:CosmicDealership.Vehicle.V1.RemoveVehicleRequest, context:ServerCallContext) =
        async {
            try
                match authorize req.User "remove:vehicles" with
                | Ok _ ->
                    let vehicleId = VehicleId.parse req.VehicleId
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
                    let vehicleId = VehicleId.parse req.VehicleId
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
                    let vehicleId = VehicleId.parse req.VehicleId
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
                | Error permissionError ->
                    return CosmicDealership.Vehicle.V1.ReturnVehicleResponse(PermissionDenied=permissionError)
            with ex ->
                Log.Error("Error! {@Exception}", ex)
                return raise ex
        } |> Async.StartAsTask
