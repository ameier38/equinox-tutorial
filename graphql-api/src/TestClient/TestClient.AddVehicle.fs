[<RequireQualifiedAccess>]
module rec TestClient.AddVehicle

type InputVariables = { input: AddVehicleInput }

type Success =
    { ///The name of the type
      __typename: string
      message: string }

type VehicleAlreadyExists =
    { ///The name of the type
      __typename: string
      message: string }

type VehicleInvalid =
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
    | VehicleAlreadyExists of vehiclealreadyexists: VehicleAlreadyExists
    | VehicleInvalid of vehicleinvalid: VehicleInvalid
    | PermissionDenied of permissiondenied: PermissionDenied

type Query =
    { /// Add a new vehicle
      addVehicle: AddVehicleResponse }
