module Server.Fields

open FSharp.Data.GraphQL.Types

let pageSizeInputField = 
    Define.Input(
        name = "pageSize", 
        typedef = Nullable Int,
        description = "Maximum number of items in a page")

let pageTokenInputField = 
    Define.Input(
        name = "pageToken", 
        typedef = Nullable ID,
        description = "Token for page to retrieve; Empty string for first page")

let vehicleIdInputField =
    Define.Input(
        name = "vehicleId",
        typedef = ID,
        description = "Unique identifier for the vehicle")

let VehicleType =
    Define.Object<Tutorial.Vehicle.V1.Vehicle>(
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
        fields = [
            vehicleIdInputField
        ])

let getVehicle
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "getVehicle",
        description = "Get the state of a vehicle",
        typedef = VehicleStateType,
        args = [Define.Input("input", GetVehicleInputType)],
        resolve = (fun ctx _ ->
            let user =
                ctx.Context.Metadata
                |> User.fromMetadata
            let input = ctx.Arg<GetVehicleInput>("input")
            vehicleClient.GetVehicle(user, input)
        ))

let AddVehicleInputType =
    Define.InputObject<AddVehicleInput>(
        name = "AddVehicleInput",
        fields = [
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
