module Server.Root

open FSharp.Data.GraphQL.Types
open Server.Vehicle.Client
open Server.Vehicle.Schema

type Root = { _empty: bool option }

type PublicRoot = { _empty: bool option }

let Query
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

let Mutation
    (vehicleClient:VehicleClient) =
    Define.Object<Root>(
        name = "Mutation",
        fields = [
            addVehicle vehicleClient
            updateVehicle vehicleClient
            removeVehicle vehicleClient
        ])
