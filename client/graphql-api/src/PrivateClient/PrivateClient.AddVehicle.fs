[<RequireQualifiedAccess>]
module rec PrivateClient.AddVehicle

type InputVariables = { input: AddVehicleInput }

type Success =
    { ///The name of the type
      __typename: string
      message: string }

type AlreadyExists =
    { ///The name of the type
      __typename: string
      message: string }

type PermissionDenied =
    { ///The name of the type
      __typename: string
      message: string }

[<RequireQualifiedAccess>]
type AddVehicleResponse =
    | Success of success: Success
    | AlreadyExists of alreadyexists: AlreadyExists
    | PermissionDenied of permissiondenied: PermissionDenied

type Query =
    { /// Add a new vehicle
      addVehicle: AddVehicleResponse }
