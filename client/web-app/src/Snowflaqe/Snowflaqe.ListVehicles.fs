[<RequireQualifiedAccess>]
module rec Snowflaqe.ListVehicles

type InputVariables = { input: ListVehiclesInput }

type VehicleState =
    { vehicleId: string
      make: string
      model: string }

/// List all the vehicles
type ListVehiclesResponse =
    { vehicles: list<VehicleState>
      nextPageToken: string
      prevPageToken: string
      totalCount: int }

type Query =
    { /// List all the vehicles
      listVehicles: ListVehiclesResponse }
