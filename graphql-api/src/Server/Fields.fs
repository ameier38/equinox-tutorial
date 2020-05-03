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

let VehicleStatusType =
    Define.Enum<Tutorial.Vehicle.V1.VehicleStatus>(
        name = "VehicleStatus",
        description = "Status of vehicle",
        options = [
            Define.EnumValue("Unknown", Tutorial.Vehicle.V1.VehicleStatus.Unspecified)
            Define.EnumValue("Available", Tutorial.Vehicle.V1.VehicleStatus.Available)
            Define.EnumValue("Removed", Tutorial.Vehicle.V1.VehicleStatus.Removed)
            Define.EnumValue("Leased", Tutorial.Vehicle.V1.VehicleStatus.Leased)
        ])

let VehicleStateType =
    Define.Object<Tutorial.Vehicle.V1.VehicleState>(
        name = "VehicleState",
        description = "State of a vehicle",
        fields = [
            Define.AutoField("vehicle", VehicleType)
            Define.AutoField("vehicleStatus", VehicleStatusType)
        ])

let ListVehiclesResponseType =
    Define.Object<Tutorial.Vehicle.V1.ListVehiclesResponse>(
        name = "ListVehiclesResponse",
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

let listVehicles
    (vehicleClient:VehicleClient) =
    Define.Field(
        name = "listVehicles",
        description = "List all the vehicles",
        typedef = ListVehiclesResponseType,
        args = [Define.Input("input", ListVehiclesInputType)],
        resolve = (fun ctx _ ->
            let input = ctx.Arg<ListVehiclesInput>("input")
            vehicleClient.ListVehicles(input)
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
            let input = ctx.Arg<GetVehicleInput>("input")
            vehicleClient.GetVehicle(input)
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
            let input = ctx.Arg<AddVehicleInput>("input")
            vehicleClient.AddVehicle(input)
        ))
