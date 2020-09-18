module GraphqlApi.Root

open FSharp.Data.GraphQL.Types
open GraphqlApi.Vehicle.Client
open GraphqlApi.Vehicle.Schema

type Root = { _empty: bool option }

let PrivateQuery
    (vehicleClient:VehicleClient) =
    Define.Object<Root>(
        name = "Query",
        fields = [ 
            listVehicles vehicleClient
            getVehicle vehicleClient
        ])

let PublicQuery
    (vehicleClient:VehicleClient) =
    Define.Object<Root>(
        name = "PublicQuery",
        fields = [ 
            listAvailableVehicles vehicleClient
            getAvailableVehicle vehicleClient
        ])

let PrivateMutation
    (vehicleClient:VehicleClient) =
    Define.Object<Root>(
        name = "Mutation",
        fields = [
            addVehicle vehicleClient
            updateVehicle vehicleClient
            addVehicleImage vehicleClient
            removeVehicle vehicleClient
        ])
