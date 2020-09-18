[<RequireQualifiedAccess>]
module rec PrivateClient.UpdateVehicle

type InputVariables = { input: UpdateVehicleInput }

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
type UpdateVehicleResponse =
    | Success of success: Success
    | NotFound of notfound: NotFound
    | PermissionDenied of permissiondenied: PermissionDenied

type Query =
    { /// Update a vehicle
      updateVehicle: UpdateVehicleResponse }
