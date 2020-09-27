[<RequireQualifiedAccess>]
module rec PrivateClient.RemoveVehicle

type InputVariables = { input: RemoveVehicleInput }

type Success =
    { ///The name of the type
      __typename: string
      message: string }

type VehicleNotFound =
    { ///The name of the type
      __typename: string
      message: string }

type PermissionDenied =
    { ///The name of the type
      __typename: string
      message: string }

[<RequireQualifiedAccess>]
type RemoveVehicleResponse =
    | Success of success: Success
    | VehicleNotFound of vehiclenotfound: VehicleNotFound
    | PermissionDenied of permissiondenied: PermissionDenied

type Query =
    { /// Remove a vehicle
      removeVehicle: RemoveVehicleResponse }
