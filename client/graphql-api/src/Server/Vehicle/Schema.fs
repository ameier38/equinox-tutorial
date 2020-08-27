module Server.Vehicle.Schema

open Server
open Server.Common.Types
open Server.Common.Schema
open Server.Vehicle.Types
open Server.Vehicle.Client
open FSharp.Data.GraphQL.Types

let vehicleIdInputField =
    Define.Input(
        name = "vehicleId",
        typedef = ID,
        description = "Unique identifier for the vehicle")

let VehicleType =
    Define.Object<Vehicle>(
        name = "Vehicle",
        description = "A space vehicle",
        fields = [
            Define.Field("_id", ID, fun _ v -> v._id.ToString())
            Define.AutoField("vehicleId", ID)
            Define.AutoField("make", String)
            Define.AutoField("model", String)
            Define.AutoField("year", Int)
            Define.AutoField("status", String)
        ])

let VehiclesType =
    Define.Object<Vehicles>(
        name = "Vehicles",
        fields = [
            Define.AutoField("vehicles", ListOf VehicleType)
            Define.AutoField("totalCount", Int)
            Define.AutoField("prevPageToken", ID)
            Define.AutoField("nextPageToken", ID)
        ])

let ListVehiclesInputType =
    Define.InputObject<ListVehiclesInput>(
        name = "ListVehiclesInput",
        fields = [
            pageTokenInputField
            pageSizeInputField
        ])

let ListVehiclesResponseType =
    Define.Union(
        name = "ListVehiclesResponse",
        options = [VehiclesType; PermissionDeniedType],
        resolveValue = (fun o ->
            match o with
            | ListVehiclesResponse.Data vehicles -> box vehicles
            | ListVehiclesResponse.PermissionDenied msg -> box msg
        ),
        resolveType = (fun o ->
            match o with
            | ListVehiclesResponse.Data _ -> upcast VehiclesType
            | ListVehiclesResponse.PermissionDenied _ -> upcast PermissionDeniedType
        )
    )

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

let listAvailableVehicles
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "listAvailableVehicles",
        description = "List available vehicles",
        typedef = VehiclesType,
        args = [Define.Input("input", ListVehiclesInputType)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<ListVehiclesInput>("input")
            vehicleClient.ListAvailableVehicles(input)
        ))

let GetVehicleInputType =
    Define.InputObject<GetVehicleInput>(
        name = "GetVehicleInput",
        fields = [vehicleIdInputField])

let GetVehicleResponseType =
    Define.Union<GetVehicleResponse,obj>(
        name = "GetVehicleResponse",
        options = [VehicleType; NotFoundType; PermissionDeniedType],
        resolveValue = (fun res ->
            match res with
            | GetVehicleResponse.Data vehicle -> box vehicle
            | GetVehicleResponse.NotFound msg -> box msg
            | GetVehicleResponse.PermissionDenied msg -> box msg
        ),
        resolveType = (fun res ->
            match res with
            | GetVehicleResponse.Data _ -> upcast VehicleType
            | GetVehicleResponse.NotFound _ -> upcast NotFoundType
            | GetVehicleResponse.PermissionDenied _ -> upcast PermissionDeniedType
        ))

let GetAvailableVehicleResponseType =
    Define.Union<GetAvailableVehicleResponse,obj>(
        name = "GetAvailableVehicleResponse",
        options = [VehicleType; NotFoundType; PermissionDeniedType],
        resolveValue = (fun res ->
            match res with
            | GetAvailableVehicleResponse.Data vehicle -> box vehicle
            | GetAvailableVehicleResponse.NotFound msg -> box msg
        ),
        resolveType = (fun res ->
            match res with
            | GetAvailableVehicleResponse.Data _ -> upcast VehicleType
            | GetAvailableVehicleResponse.NotFound _ -> upcast NotFoundType
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
        )
    )

let AddVehicleInputType =
    Define.InputObject<AddVehicleInput>(
        name = "AddVehicleInput",
        fields = [
            vehicleIdInputField
            Define.Input("make", String)
            Define.Input("model", String)
            Define.Input("year", Int)
        ])

let AddVehicleResponseType =
    Define.Union<AddVehicleResponse, obj>(
        name = "AddVehicleResponse",
        options = [SuccessType; PermissionDeniedType; AlreadyExistsType],
        resolveValue = (fun o ->
            match o with
            | AddVehicleResponse.Success msg
            | AddVehicleResponse.PermissionDenied msg
            | AddVehicleResponse.AlreadyExists msg -> box msg
        ),
        resolveType = (fun o ->
            match o with
            | AddVehicleResponse.Success _ -> upcast SuccessType
            | AddVehicleResponse.PermissionDenied _ -> upcast PermissionDeniedType
            | AddVehicleResponse.AlreadyExists _ -> upcast AlreadyExistsType
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
            let input = ctx.Arg<AddVehicleInput>("input")
            vehicleClient.AddVehicle(user, input)
        ))

let UpdateVehicleInputType = 
    Define.InputObject<UpdateVehicleInput>(
        name = "UpdateVehicleInput",
        fields = [
            vehicleIdInputField
            Define.Input("make", Nullable String)
            Define.Input("model", Nullable String)
            Define.Input("year", Nullable Int)
        ])

let UpdateVehicleResponseType =
    Define.Union<UpdateVehicleResponse, obj>(
        name = "UpdateVehicleResponse",
        options = [SuccessType; PermissionDeniedType; NotFoundType],
        resolveValue = (fun o ->
            match o with
            | UpdateVehicleResponse.Success msg
            | UpdateVehicleResponse.NotFound msg
            | UpdateVehicleResponse.PermissionDenied msg -> box msg
        ),
        resolveType = (fun o ->
            match o with
            | UpdateVehicleResponse.Success _ -> upcast SuccessType
            | UpdateVehicleResponse.NotFound _ -> upcast NotFoundType
            | UpdateVehicleResponse.PermissionDenied _ -> upcast PermissionDeniedType
        )
    )

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
            let input = ctx.Arg<UpdateVehicleInput>("input")
            vehicleClient.UpdateVehicle(user, input)
        ))

let RemoveVehicleInputType =
    Define.InputObject<RemoveVehicleInput>(
        name = "RemoveVehicleInput",
        fields = [vehicleIdInputField])

let RemoveVehicleResponseType =
    Define.Union<RemoveVehicleResponse, obj>(
        name = "RemoveVehicleResponse",
        options = [SuccessType; NotFoundType; PermissionDeniedType],
        resolveValue = (fun o ->
            match o with
            | RemoveVehicleResponse.Success msg
            | RemoveVehicleResponse.NotFound msg
            | RemoveVehicleResponse.PermissionDenied msg -> box msg
        ),
        resolveType = (fun o ->
            match o with
            | RemoveVehicleResponse.Success _ -> upcast SuccessType
            | RemoveVehicleResponse.NotFound _ -> upcast NotFoundType
            | RemoveVehicleResponse.PermissionDenied _ -> upcast PermissionDeniedType
        )
    )

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