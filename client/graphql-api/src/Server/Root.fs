module Server.Root

open FSharp.Data.GraphQL.Types

type Root = { _empty: bool option }

let Query
    (vehicleClient:VehicleClient) =
    Define.Object<Root>(
        name = "Query",
        fields = [ 
            Fields.listVehicles vehicleClient
            Fields.getVehicle vehicleClient
        ]
    )

let Mutation
    (vehicleClient:VehicleClient) =
    Define.Object<Root>(
        name = "Mutation",
        fields = [
            Fields.addVehicle vehicleClient
        ]
    )
