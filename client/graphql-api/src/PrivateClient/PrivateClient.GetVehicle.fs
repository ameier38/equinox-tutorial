[<RequireQualifiedAccess>]
module rec PrivateClient.GetVehicle

type InputVariables = { input: GetVehicleInput }

type NotFound =
    { ///The name of the type
      __typename: string
      message: string }

type PermissionDenied =
    { ///The name of the type
      __typename: string
      message: string }

/// A space vehicle
type Vehicle =
    { ///The name of the type
      __typename: string
      vehicleId: string
      make: string
      model: string
      year: int
      status: string }

[<RequireQualifiedAccess>]
type GetVehicleResponse =
    | NotFound of notfound: NotFound
    | PermissionDenied of permissiondenied: PermissionDenied
    | Vehicle of vehicle: Vehicle

type Query =
    { /// Get the state of a vehicle
      getVehicle: GetVehicleResponse }