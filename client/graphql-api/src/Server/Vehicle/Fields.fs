module Server.Vehicle.Fields

open Server
open Server.Fields
open Server.Vehicle.Types
open Server.Vehicle.Client
open FSharp.Data.GraphQL.Types

let vehicleIdInputField =
    Define.Input(
        name = "vehicleId",
        typedef = ID,
        description = "Unique identifier for the vehicle")

let VehicleType =
    Define.Object<CosmicDealership.Vehicle.V1.Vehicle>(
        name = "Vehicle",
        description = "Space vehicle",
        fields = [
            Define.AutoField("vehicleId", ID)
            Define.AutoField("make", String)
            Define.AutoField("model", String)
            Define.AutoField("year", Int)
        ])

let VehicleStateType =
    Define.Object<VehicleState>(
        name = "VehicleState",
        description = "State of a vehicle",
        fields = [
            Define.Field("_id", ID, fun _ v -> v._id.ToString())
            Define.AutoField("vehicleId", ID)
            Define.AutoField("make", String)
            Define.AutoField("model", String)
            Define.AutoField("year", Int)
            Define.AutoField("status", String)
        ])

let ListVehiclesResponseType =
    Define.Object<ListVehiclesResponse>(
        name = "ListVehiclesResponse",
        fields = [
            Define.AutoField("vehicles", ListOf VehicleStateType)
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
        typedef = ListVehiclesResponseType,
        args = [Define.Input("input", ListVehiclesInputType)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<ListVehiclesInput>("input")
            vehicleClient.ListAvailableVehicles(input)
        ))

let GetVehicleInputType =
    Define.InputObject<GetVehicleInput>(
        name = "GetVehicleInput",
        fields = [vehicleIdInputField])

let VehicleNotFoundType =
    Define.Object<VehicleNotFound>(
        name = "VehicleNotFound",
        fields = [
            Define.AutoField("message", String)
        ])

let GetVehicleResponseType =
    Define.Union(
        name = "GetVehicleResponse",
        options = [VehicleStateType; VehicleNotFoundType],
        resolveValue = (fun res ->
            match res with
            | Found vehicleState -> box vehicleState
            | NotFound msg -> box msg),
        resolveType = (fun res ->
            match res with
            | Found _ -> upcast VehicleStateType
            | NotFound _ -> upcast VehicleNotFoundType))

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
            match vehicleClient.GetVehicle(user, input) with
            | Some vehicleState -> Found vehicleState
            | None -> NotFound { message = sprintf "Could not find Vehicle-%s" input.vehicleId }
        ))

let getAvailableVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "getAvailableVehicle",
        description = "Get the state of an available vehicle",
        typedef = GetVehicleResponseType,
        args = [Define.Input("input", GetVehicleInputType)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<GetVehicleInput>("input")
            match vehicleClient.GetAvailableVehicle(input) with
            | Some vehicleState -> Found vehicleState
            | None -> NotFound { message = sprintf "Could not find available Vehicle-%s" input.vehicleId }
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

let addVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "addVehicle",
        description = "Add a new vehicle",
        typedef = String,
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

let updateVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "updateVehicle",
        description = "Update a vehicle",
        typedef = String,
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

let removeVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "removeVehicle",
        description = "Remove a vehicle",
        typedef = String,
        args = [Define.Input("input", RemoveVehicleInputType)],
        resolve = (fun ctx _ ->
            let user = 
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<RemoveVehicleInput>("input")
            vehicleClient.RemoveVehicle(user, input)
        ))