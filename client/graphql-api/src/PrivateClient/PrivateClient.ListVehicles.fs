[<RequireQualifiedAccess>]
module rec PrivateClient.ListVehicles

type InputVariables = { input: ListVehiclesInput }

type PermissionDenied =
    { ///The name of the type
      __typename: string
      message: string }

type Vehicle =
    { vehicleId: string
      make: string
      model: string
      year: int }

type Vehicles =
    { ///The name of the type
      __typename: string
      totalCount: int
      prevPageToken: string
      nextPageToken: string
      vehicles: list<Vehicle> }

[<RequireQualifiedAccess>]
type ListVehiclesResponse =
    | PermissionDenied of permissiondenied: PermissionDenied
    | Vehicles of vehicles: Vehicles

type Query =
    { /// List all vehicles
      listVehicles: ListVehiclesResponse }
