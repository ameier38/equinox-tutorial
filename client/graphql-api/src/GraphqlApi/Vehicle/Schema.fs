module GraphqlApi.Vehicle.Schema

open GraphqlApi
open GraphqlApi.Common.Types
open GraphqlApi.Common.Schema
open GraphqlApi.Vehicle.Types
open GraphqlApi.Vehicle.Client
open FSharp.Data.GraphQL.Types

let PageTokenInvalidType =
    Define.Object<Message>(name = "PageTokenInvalid", fields = [ Define.AutoField("message", String) ])

let PageSizeInvalidType =
    Define.Object<Message>(name = "PageSizeInvalid", fields = [ Define.AutoField("message", String) ])

let VehicleAlreadyExistsType =
    Define.Object<Message>(name = "VehicleAlreadyExists", fields = [ Define.AutoField("message", String) ])

let VehicleInvalidType =
    Define.Object<Message>(name = "VehicleInvalid", fields = [ Define.AutoField("message", String) ])

let VehicleNotFoundType =
    Define.Object<Message>(name = "VehicleNotFound", fields = [ Define.AutoField("message", String) ])

let VehicleCurrentlyLeasedType =
    Define.Object<Message>(name = "VehicleCurrentlyLeased", fields = [ Define.AutoField("message", String) ])

let VehicleAlreadyReturnedType =
    Define.Object<Message>(name = "VehicleAlreadyReturned", fields = [ Define.AutoField("message", String) ])

let AvatarInvalidType =
    Define.Object<Message>(name = "AvatarInvalid", fields = [ Define.AutoField("message", String) ])

let ImageInvalidType =
    Define.Object<Message>(name = "ImageInvalid", fields = [ Define.AutoField("message", String) ])

let MaxImageCountReachedType =
    Define.Object<Message>(name = "MaxImageCountReached", fields = [ Define.AutoField("message", String) ])

let ImageNotFoundType =
    Define.Object<Message>(name = "ImageNotFound", fields = [ Define.AutoField("message", String) ])

let vehicleIdInputField =
    Define.Input(
        name = "vehicleId",
        typedef = ID,
        description = "Unique identifier for the vehicle")

let VehicleStatusType =
    Define.Enum<CosmicDealership.Vehicle.V1.VehicleStatus>(
        name="VehicleStatus",
        description="Status of a vehicle",
        options=[
            Define.EnumValue("Unknown", CosmicDealership.Vehicle.V1.VehicleStatus.Unspecified)
            Define.EnumValue("Available", CosmicDealership.Vehicle.V1.VehicleStatus.Available)
            Define.EnumValue("Leased", CosmicDealership.Vehicle.V1.VehicleStatus.Leased)
        ])

let VehicleType =
    Define.Object<CosmicDealership.Vehicle.V1.Vehicle>(
        name="Vehicle",
        description="A vehicle",
        fields=[
            Define.AutoField("make", String)
            Define.AutoField("model", String)
            Define.AutoField("year", Int)
        ])

let InventoriedVehicleType =
    Define.Object<CosmicDealership.Vehicle.V1.InventoriedVehicle>(
        name = "InventoriedVehicle",
        description = "A vehicle in inventory",
        fields = [
            Define.AutoField("vehicleId", ID)
            Define.Field("addedAt", Date, resolve=(fun _ v -> v.AddedAt.ToDateTime()))
            Define.Field("updatedAt", Date, resolve=(fun _ v -> v.UpdatedAt.ToDateTime()))
            Define.AutoField("vehicle", VehicleType)
            Define.AutoField("status", VehicleStatusType)
            Define.AutoField("avatar", String)
            Define.AutoField("images", ListOf String)
        ])

let ListVehiclesInputType =
    Define.InputObject<ListVehiclesInput>(
        name = "ListVehiclesInput",
        fields = [
            pageTokenInputField
            pageSizeInputField
        ])

let ListVehiclesSuccessType =
    Define.Object<CosmicDealership.Vehicle.V1.ListVehiclesSuccess>(
        name="ListVehiclesSuccess",
        description="Successful list vehicles response",
        fields=[
            Define.AutoField("vehicles", ListOf InventoriedVehicleType)
            Define.AutoField("totalCount", Int)
            Define.AutoField("nextPageToken", ID)
        ])

type ListVehiclesResponseCase = CosmicDealership.Vehicle.V1.ListVehiclesResponse.ResponseOneofCase

let ListVehiclesResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.ListVehiclesResponse, obj>(
        name = "ListVehiclesResponse",
        options = [ListVehiclesSuccessType; PageTokenInvalidType; PageSizeInvalidType; PermissionDeniedType],
        resolveType = (fun res ->
            match res.ResponseCase with
            | ListVehiclesResponseCase.Success -> upcast ListVehiclesSuccessType
            | ListVehiclesResponseCase.PageTokenInvalid -> upcast PageTokenInvalidType
            | ListVehiclesResponseCase.PageSizeInvalid -> upcast PageSizeInvalidType
            | ListVehiclesResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid ListVehiclesResponse: %A" other
        ),
        resolveValue = (fun res ->
            match res.ResponseCase with
            | ListVehiclesResponseCase.Success -> box res.Success
            | ListVehiclesResponseCase.PageTokenInvalid -> box { message = res.PageTokenInvalid }
            | ListVehiclesResponseCase.PageSizeInvalid -> box { message = res.PageSizeInvalid }
            | ListVehiclesResponseCase.PermissionDenied -> box { message = res.PermissionDenied }
            | other -> failwithf "invalid ListVehiclesResponse: %A" other
        ))

let listVehicles
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "listVehicles",
        description = "List all vehicles",
        typedef = ListVehiclesResponseType,
        args = [Define.Input("input", ListVehiclesInputType)],
        resolve = (fun ctx _ ->
            let user =
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<ListVehiclesInput>("input")
            vehicleClient.ListVehicles(user, input)
        ))

type ListAvailableVehiclesResponseCase = CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse.ResponseOneofCase

let ListAvailableVehiclesResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse, obj>(
        name = "ListAvailableVehiclesResponse",
        options = [ListVehiclesSuccessType; PageTokenInvalidType; PageSizeInvalidType],
        resolveType = (fun res ->
            match res.ResponseCase with
            | ListAvailableVehiclesResponseCase.Success -> upcast ListVehiclesSuccessType
            | ListAvailableVehiclesResponseCase.PageTokenInvalid -> upcast PageTokenInvalidType
            | ListAvailableVehiclesResponseCase.PageSizeInvalid -> upcast PageSizeInvalidType
            | other -> failwithf "invalid ListAvailableVehiclesResponse: %A" other
        ),
        resolveValue = (fun res ->
            match res.ResponseCase with
            | ListAvailableVehiclesResponseCase.Success -> box res.Success
            | ListAvailableVehiclesResponseCase.PageTokenInvalid -> box { message = res.PageTokenInvalid }
            | ListAvailableVehiclesResponseCase.PageSizeInvalid -> box { message = res.PageSizeInvalid }
            | other -> failwithf "invalid ListAvailableVehiclesResponse: %A" other
        ))

let listAvailableVehicles
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "listAvailableVehicles",
        description = "List available vehicles",
        typedef = ListAvailableVehiclesResponseType,
        args = [Define.Input("input", ListVehiclesInputType)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<ListVehiclesInput>("input")
            vehicleClient.ListAvailableVehicles(input)
        ))

let GetVehicleInputType =
    Define.InputObject<GetVehicleInput>(
        name = "GetVehicleInput",
        fields = [vehicleIdInputField])

type GetVehicleResponseCase = CosmicDealership.Vehicle.V1.GetVehicleResponse.ResponseOneofCase

let GetVehicleResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.GetVehicleResponse,obj>(
        name = "GetVehicleResponse",
        options = [InventoriedVehicleType; VehicleNotFoundType; PermissionDeniedType],
        resolveType = (fun res ->
            match res.ResponseCase with
            | GetVehicleResponseCase.Success -> upcast InventoriedVehicleType
            | GetVehicleResponseCase.VehicleNotFound -> upcast VehicleNotFoundType
            | GetVehicleResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid GetVehicleResponse: %A" other
        ),
        resolveValue = (fun res ->
            match res.ResponseCase with
            | GetVehicleResponseCase.Success -> box res.Success
            | GetVehicleResponseCase.VehicleNotFound -> box { message = res.VehicleNotFound }
            | GetVehicleResponseCase.PermissionDenied -> box { message = res.PermissionDenied }
            | other -> failwithf "invalid GetVehicleResponse: %A" other
        ))

let getVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "getVehicle",
        description = "Get the state of a vehicle",
        typedef = GetVehicleResponseType,
        args = [Define.Input("input", GetVehicleInputType)],
        resolve = (fun ctx _ ->
            let user =
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<GetVehicleInput>("input")
            vehicleClient.GetVehicle(user, input)
        ))

type GetAvailableVehicleResponseCase = CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse.ResponseOneofCase

let GetAvailableVehicleResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.GetAvailableVehicleResponse,obj>(
        name = "GetAvailableVehicleResponse",
        options = [InventoriedVehicleType; VehicleNotFoundType],
        resolveType = (fun res ->
            match res.ResponseCase with
            | GetAvailableVehicleResponseCase.Success -> upcast InventoriedVehicleType
            | GetAvailableVehicleResponseCase.VehicleNotFound -> upcast VehicleNotFoundType
            | other -> failwithf "invalid GetAvailableVehicleResponse: %A" other
        ),
        resolveValue = (fun res ->
            match res.ResponseCase with
            | GetAvailableVehicleResponseCase.Success -> box res.Success
            | GetAvailableVehicleResponseCase.VehicleNotFound -> box { message = res.VehicleNotFound }
            | other -> failwithf "invalid GetAvailableVehicleResponse: %A" other
        ))


let getAvailableVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "getAvailableVehicle",
        description = "Get the state of an available vehicle",
        typedef = GetAvailableVehicleResponseType,
        args = [Define.Input("input", GetVehicleInputType)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<GetVehicleInput>("input")
            vehicleClient.GetAvailableVehicle(input)
        ))

let AddVehicleInputType =
    Define.InputObject<VehicleInput>(
        name = "AddVehicleInput",
        fields = [
            vehicleIdInputField
            Define.Input("make", String)
            Define.Input("model", String)
            Define.Input("year", Int)
        ])

type AddVehicleResponseCase = CosmicDealership.Vehicle.V1.AddVehicleResponse.ResponseOneofCase

let AddVehicleResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.AddVehicleResponse, obj>(
        name = "AddVehicleResponse",
        options = [SuccessType; VehicleAlreadyExistsType; VehicleInvalidType; PermissionDeniedType],
        resolveType = (fun o ->
            match o.ResponseCase with
            | AddVehicleResponseCase.Success -> upcast SuccessType
            | AddVehicleResponseCase.VehicleAlreadyExists -> upcast VehicleAlreadyExistsType
            | AddVehicleResponseCase.VehicleInvalid -> upcast VehicleInvalidType
            | AddVehicleResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid response case: %A" other
        ),
        resolveValue = (fun o ->
            match o.ResponseCase with
            | AddVehicleResponseCase.Success -> box { message = o.Success }
            | AddVehicleResponseCase.VehicleAlreadyExists -> box { message = o.VehicleAlreadyExists }
            | AddVehicleResponseCase.VehicleInvalid -> box { message = o.VehicleInvalid }
            | AddVehicleResponseCase.PermissionDenied -> box { message = o.PermissionDenied }
            | other -> failwithf "invalid response case: %A" other
        )
    )

let addVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "addVehicle",
        description = "Add a new vehicle",
        typedef = AddVehicleResponseType,
        args = [Define.Input("input", AddVehicleInputType)],
        resolve = (fun ctx _ ->
            let user =
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<VehicleInput>("input")
            vehicleClient.AddVehicle(user, input)
        ))

let UpdateVehicleInputType = 
    Define.InputObject<VehicleInput>(
        name = "UpdateVehicleInput",
        fields = [
            vehicleIdInputField
            Define.Input("make", String)
            Define.Input("model", String)
            Define.Input("year", Int)
        ])

type UpdateVehicleResponseCase = CosmicDealership.Vehicle.V1.UpdateVehicleResponse.ResponseOneofCase

let UpdateVehicleResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.UpdateVehicleResponse, obj>(
        name = "UpdateVehicleResponse",
        options = [SuccessType; VehicleNotFoundType; VehicleInvalidType; PermissionDeniedType],
        resolveType = (fun o ->
            match o.ResponseCase with
            | UpdateVehicleResponseCase.Success -> upcast SuccessType
            | UpdateVehicleResponseCase.VehicleNotFound -> upcast VehicleNotFoundType
            | UpdateVehicleResponseCase.VehicleInvalid -> upcast VehicleInvalidType
            | UpdateVehicleResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid response case: %A" other
        ),
        resolveValue = (fun o ->
            match o.ResponseCase with
            | UpdateVehicleResponseCase.Success -> box { message = o.Success }
            | UpdateVehicleResponseCase.VehicleNotFound -> box { message = o.VehicleNotFound }
            | UpdateVehicleResponseCase.VehicleInvalid -> box { message = o.VehicleInvalid }
            | UpdateVehicleResponseCase.PermissionDenied -> box { message = o.PermissionDenied }
            | other -> failwithf "invalid response case: %A" other
        ))

let updateVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "updateVehicle",
        description = "Update a vehicle",
        typedef = UpdateVehicleResponseType,
        args = [Define.Input("input", UpdateVehicleInputType)],
        resolve = (fun ctx _ ->
            let user = 
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<VehicleInput>("input")
            vehicleClient.UpdateVehicle(user, input)
        ))

let UpdateVehicleAvatarInputType =
    Define.InputObject<UpdateVehicleAvatarInput>(
        name="UpdateVehicleAvatarInput",
        fields=[
            vehicleIdInputField
            Define.Input("avatarUrl", String)
        ])

type UpdateVehicleAvatarResponseCase = CosmicDealership.Vehicle.V1.UpdateAvatarResponse.ResponseOneofCase

let UpdateVehicleAvatarResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.UpdateAvatarResponse, obj>(
        name="UpdateVehicleAvatarResponse",
        options=[SuccessType; VehicleNotFoundType; AvatarInvalidType; PermissionDeniedType],
        resolveType=(fun o ->
            match o.ResponseCase with
            | UpdateVehicleAvatarResponseCase.Success -> upcast SuccessType
            | UpdateVehicleAvatarResponseCase.VehicleNotFound -> upcast VehicleNotFoundType
            | UpdateVehicleAvatarResponseCase.AvatarInvalid -> upcast AvatarInvalidType
            | UpdateVehicleAvatarResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid response case: %A" other
        ),
        resolveValue=(fun o ->
            match o.ResponseCase with
            | UpdateVehicleAvatarResponseCase.Success -> box { message = o.Success }
            | UpdateVehicleAvatarResponseCase.VehicleNotFound -> box { message = o.VehicleNotFound } 
            | UpdateVehicleAvatarResponseCase.AvatarInvalid -> box { message = o.AvatarInvalid }
            | UpdateVehicleAvatarResponseCase.PermissionDenied -> box { message = o.PermissionDenied }
            | other -> failwithf "invalid response case: %A" other
        ))

let updateVehicleAvatar
    (vehicleClient:VehicleClient) =
    Define.Field(
        name="updateVehicleAvatar",
        description="Update a vehicle's avatar",
        typedef=UpdateVehicleAvatarResponseType,
        args = [Define.Input("input", UpdateVehicleAvatarInputType)],
        resolve = (fun ctx _ ->
            let user = 
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<UpdateVehicleAvatarInput>("input")
            vehicleClient.UpdateAvatar(user, input)
        ))

let RemoveVehicleAvatarInputType =
    Define.InputObject<RemoveVehicleAvatarInput>(
        name="RemoveVehicleAvatarInput",
        fields=[
            vehicleIdInputField
        ])

type RemoveVehicleAvatarResponseCase = CosmicDealership.Vehicle.V1.RemoveAvatarResponse.ResponseOneofCase

let RemoveVehicleAvatarResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.RemoveAvatarResponse, obj>(
        name="RemoveVehicleAvatarResponse",
        options=[SuccessType; VehicleNotFoundType; PermissionDeniedType],
        resolveType=(fun o ->
            match o.ResponseCase with
            | RemoveVehicleAvatarResponseCase.Success -> upcast SuccessType
            | RemoveVehicleAvatarResponseCase.VehicleNotFound -> upcast VehicleNotFoundType
            | RemoveVehicleAvatarResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid response case: %A" other
        ),
        resolveValue=(fun o ->
            match o.ResponseCase with
            | RemoveVehicleAvatarResponseCase.Success -> box { message = o.Success }
            | RemoveVehicleAvatarResponseCase.VehicleNotFound -> box { message = o.VehicleNotFound } 
            | RemoveVehicleAvatarResponseCase.PermissionDenied -> box { message = o.PermissionDenied }
            | other -> failwithf "invalid response case: %A" other
        ))

let removeVehicleAvatar
    (vehicleClient:VehicleClient) =
    Define.Field(
        name="removeVehicleAvatar",
        description="Remove a vehicle's avatar",
        typedef=RemoveVehicleAvatarResponseType,
        args = [Define.Input("input", RemoveVehicleAvatarInputType)],
        resolve = (fun ctx _ ->
            let user = 
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<RemoveVehicleAvatarInput>("input")
            vehicleClient.RemoveAvatar(user, input)
        ))

let AddVehicleImageInputType =
    Define.InputObject<AddVehicleImageInput>(
        name = "AddVehicleImageInput",
        fields = [
            vehicleIdInputField
            Define.Input("imageUrl", String)
        ])

type AddVehicleImageResponseCase = CosmicDealership.Vehicle.V1.AddImageResponse.ResponseOneofCase

let AddVehicleImageResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.AddImageResponse, obj>(
        name  = "AddVehicleImageResponse",
        options = [SuccessType; VehicleNotFoundType; ImageInvalidType; MaxImageCountReachedType; PermissionDeniedType],
        resolveType = (fun o ->
            match o.ResponseCase with
            | AddVehicleImageResponseCase.Success -> upcast SuccessType
            | AddVehicleImageResponseCase.VehicleNotFound -> upcast VehicleNotFoundType
            | AddVehicleImageResponseCase.ImageInvalid -> upcast ImageInvalidType
            | AddVehicleImageResponseCase.MaxImageCountReached -> upcast MaxImageCountReachedType
            | AddVehicleImageResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid response case: %A" other
        ),
        resolveValue = (fun o ->
            match o.ResponseCase with
            | AddVehicleImageResponseCase.Success -> box { message = o.Success }
            | AddVehicleImageResponseCase.VehicleNotFound -> box { message = o.VehicleNotFound }
            | AddVehicleImageResponseCase.ImageInvalid -> box { message = o.ImageInvalid }
            | AddVehicleImageResponseCase.MaxImageCountReached -> box { message = o.MaxImageCountReached }
            | AddVehicleImageResponseCase.PermissionDenied -> box { message = o.PermissionDenied }
            | other -> failwithf "invalid response case: %A" other
        )
    )

let addVehicleImage
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "addVehicleImage",
        description = "Add image of vehicle",
        typedef = AddVehicleImageResponseType,
        args = [Define.Input("input", AddVehicleImageInputType)],
        resolve = (fun ctx _ ->
            let user = 
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<AddVehicleImageInput>("input")
            vehicleClient.AddImage(user, input)
        ))

let RemoveVehicleImageInputType =
    Define.InputObject<RemoveVehicleImageInput>(
        name="RemoveVehicleImageInput",
        fields=[
            vehicleIdInputField
            Define.Input("imageUrl", String)
        ])

type RemoveVehicleImageResponseCase = CosmicDealership.Vehicle.V1.RemoveImageResponse.ResponseOneofCase

let RemoveVehicleImageResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.RemoveImageResponse, obj>(
        name="RemoveVehicleImageResponse",
        options=[SuccessType; VehicleNotFoundType; ImageNotFoundType; ImageInvalidType; PermissionDeniedType],
        resolveType=(fun o ->
            match o.ResponseCase with
            | RemoveVehicleImageResponseCase.Success -> upcast SuccessType
            | RemoveVehicleImageResponseCase.VehicleNotFound -> upcast VehicleNotFoundType
            | RemoveVehicleImageResponseCase.ImageNotFound -> upcast ImageNotFoundType
            | RemoveVehicleImageResponseCase.ImageInvalid -> upcast ImageInvalidType
            | RemoveVehicleImageResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid response case: %A" other
        ),
        resolveValue=(fun o ->
            match o.ResponseCase with
            | RemoveVehicleImageResponseCase.Success -> box { message = o.Success }
            | RemoveVehicleImageResponseCase.VehicleNotFound -> box { message = o.VehicleNotFound }
            | RemoveVehicleImageResponseCase.ImageNotFound -> box { message = o.ImageNotFound }
            | RemoveVehicleImageResponseCase.ImageInvalid -> box { message = o.ImageInvalid }
            | RemoveVehicleImageResponseCase.PermissionDenied -> box { message = o.PermissionDenied }
            | other -> failwithf "invalid response case: %A" other
        ))

let removeVehicleImage
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "removeVehicleImage",
        description = "Remove a vehicle image",
        typedef = RemoveVehicleImageResponseType,
        args = [Define.Input("input", RemoveVehicleImageInputType)],
        resolve = (fun ctx _ ->
            let user = 
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<RemoveVehicleImageInput>("input")
            vehicleClient.RemoveImage(user, input)
        ))

let RemoveVehicleInputType =
    Define.InputObject<RemoveVehicleInput>(
        name = "RemoveVehicleInput",
        fields = [vehicleIdInputField])

type RemoveVehicleResponseCase = CosmicDealership.Vehicle.V1.RemoveVehicleResponse.ResponseOneofCase

let RemoveVehicleResponseType =
    Define.Union<CosmicDealership.Vehicle.V1.RemoveVehicleResponse, obj>(
        name = "RemoveVehicleResponse",
        options = [SuccessType; VehicleNotFoundType; VehicleCurrentlyLeasedType; PermissionDeniedType],
        resolveType = (fun o ->
            match o.ResponseCase with
            | RemoveVehicleResponseCase.Success -> upcast SuccessType
            | RemoveVehicleResponseCase.VehicleNotFound -> upcast VehicleNotFoundType
            | RemoveVehicleResponseCase.VehicleCurrentlyLeased -> upcast VehicleCurrentlyLeasedType
            | RemoveVehicleResponseCase.PermissionDenied -> upcast PermissionDeniedType
            | other -> failwithf "invalid response case: %A" other
        ),
        resolveValue = (fun o ->
            match o.ResponseCase with
            | RemoveVehicleResponseCase.Success -> box { message = o.Success }
            | RemoveVehicleResponseCase.VehicleNotFound -> box { message = o.VehicleNotFound }
            | RemoveVehicleResponseCase.VehicleCurrentlyLeased -> box { message = o.VehicleCurrentlyLeased }
            | RemoveVehicleResponseCase.PermissionDenied -> box { message = o.PermissionDenied }
            | other -> failwithf "invalid response case: %A" other
        ))

let removeVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "removeVehicle",
        description = "Remove a vehicle",
        typedef = RemoveVehicleResponseType,
        args = [Define.Input("input", RemoveVehicleInputType)],
        resolve = (fun ctx _ ->
            let user = 
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<RemoveVehicleInput>("input")
            vehicleClient.RemoveVehicle(user, input)
        ))