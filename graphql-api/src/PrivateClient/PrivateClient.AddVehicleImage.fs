[<RequireQualifiedAccess>]
module rec PrivateClient.AddVehicleImage

type InputVariables = { input: AddVehicleImageInput }

type Success =
    { ///The name of the type
      __typename: string
      message: string }

type VehicleNotFound =
    { ///The name of the type
      __typename: string
      message: string }

type ImageInvalid =
    { ///The name of the type
      __typename: string
      message: string }

type MaxImageCountReached =
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
    | VehicleNotFound of vehiclenotfound: VehicleNotFound
    | ImageInvalid of imageinvalid: ImageInvalid
    | MaxImageCountReached of maximagecountreached: MaxImageCountReached
    | PermissionDenied of permissiondenied: PermissionDenied

type Query =
    { /// Add image of vehicle
      addVehicleImage: AddVehicleImageResponse }
