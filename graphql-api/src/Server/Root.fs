module GraphqlApi.Root

open FSharp.Data.GraphQL.Types
open GraphqlApi.Vehicle.Client
open GraphqlApi.Vehicle.Schema

type Root = { _empty: bool option }

let Query
    (vehicleClient:VehicleClient) =
    Define.Object<Root>(
        name = "Query",
        fields = [ 
            listAvailableVehicles vehicleClient
            getAvailableVehicle vehicleClient
            listVehicles vehicleClient
            getVehicle vehicleClient
        ])

let Mutation
    (vehicleClient:VehicleClient) =
    Define.Object<Root>(
        name = "Mutation",
        fields = [
            addVehicle vehicleClient
            updateVehicle vehicleClient
            updateVehicleAvatar vehicleClient
            removeVehicleAvatar vehicleClient
            addVehicleImage vehicleClient
            removeVehicleImage vehicleClient
            removeVehicle vehicleClient
        ])
