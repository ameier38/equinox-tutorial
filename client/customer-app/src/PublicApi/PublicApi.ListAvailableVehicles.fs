[<RequireQualifiedAccess>]
module rec PublicApi.ListAvailableVehicles

type InputVariables = { input: ListVehiclesInput }

type VehicleState =
    { vehicleId: string
      make: string
      model: string
      status: string }

/// List available vehicles
type ListVehiclesResponse =
    { vehicles: list<VehicleState>
      nextPageToken: string
      prevPageToken: string
      totalCount: int }

type Query =
    { /// List available vehicles
      listAvailableVehicles: ListVehiclesResponse }
