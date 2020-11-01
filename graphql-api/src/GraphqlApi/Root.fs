module GraphqlApi.Root

open FSharp.Data.GraphQL.Types
open GraphqlApi.Vehicle.Client
open GraphqlApi.Vehicle.Schema

type PrivateRoot = { _empty: bool option }
type PublicRoot = { _empty: bool option }

let PrivateQuery
    (vehicleClient:VehicleClient) =
    Define.Object<PrivateRoot>(
        name = "Query",
        fields = [ 
            listVehicles vehicleClient
            getVehicle vehicleClient
        ])

let PrivateMutation
    (vehicleClient:VehicleClient) =
    Define.Object<PrivateRoot>(
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

let PublicQuery
    (vehicleClient:VehicleClient) =
    Define.Object<PublicRoot>(
        name = "Query",
        fields = [ 
            listAvailableVehicles vehicleClient
            getAvailableVehicle vehicleClient
        ])
