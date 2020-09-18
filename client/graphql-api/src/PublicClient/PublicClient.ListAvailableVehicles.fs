[<RequireQualifiedAccess>]
module rec PublicClient.ListAvailableVehicles

type InputVariables = { input: ListVehiclesInput }

type Vehicle =
    { vehicleId: string
      make: string
      model: string
      year: int }

/// List available vehicles
type Vehicles =
    { totalCount: int
      prevPageToken: string
      nextPageToken: string
      vehicles: list<Vehicle> }

type Query =
    { /// List available vehicles
      listAvailableVehicles: Vehicles }
