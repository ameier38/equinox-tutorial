[<RequireQualifiedAccess>]
module rec PrivateClient.AddVehicleImage

type InputVariables = { input: AddVehicleImageInput }

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
type AddVehicleImageResponse =
    | Success of success: Success
    | NotFound of notfound: NotFound
    | PermissionDenied of permissiondenied: PermissionDenied

type Query =
    { /// Add image of vehicle
      addVehicleImage: AddVehicleImageResponse }
