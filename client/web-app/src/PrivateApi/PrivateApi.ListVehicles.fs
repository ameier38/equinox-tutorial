[<RequireQualifiedAccess>]
module rec PrivateApi.ListVehicles

type InputVariables = { input: ListVehiclesInput }

type VehicleState =
    { vehicleId: string
      make: string
      model: string
      year: int }

/// List all vehicles
type ListVehiclesResponse =
    { vehicles: list<VehicleState>
      totalCount: int
      nextPageToken: string
      prevPageToken: string }

type Query =
    { /// List all vehicles
      listVehicles: ListVehiclesResponse }
