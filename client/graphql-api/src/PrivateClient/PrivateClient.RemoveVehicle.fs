[<RequireQualifiedAccess>]
module rec PrivateClient.RemoveVehicle

type InputVariables = { input: RemoveVehicleInput }

type Success =
    { ///The name of the type
      __typename: string
      message: string }

type NotFound =
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
    | NotFound of notfound: NotFound
    | PermissionDenied of permissiondenied: PermissionDenied

type Query =
    { /// Remove a vehicle
      removeVehicle: RemoveVehicleResponse }
