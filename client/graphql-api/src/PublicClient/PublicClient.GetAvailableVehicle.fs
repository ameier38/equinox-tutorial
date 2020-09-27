[<RequireQualifiedAccess>]
module rec PublicClient.GetAvailableVehicle

type InputVariables = { input: GetVehicleInput }

type VehicleNotFound =
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
type GetAvailableVehicleResponse =
    | VehicleNotFound of vehiclenotfound: VehicleNotFound
    | Vehicle of vehicle: Vehicle

type Query =
    { /// Get the state of an available vehicle
      getAvailableVehicle: GetAvailableVehicleResponse }
