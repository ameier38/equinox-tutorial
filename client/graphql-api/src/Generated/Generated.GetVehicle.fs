[<RequireQualifiedAccess>]
module rec Generated.GetVehicle

type InputVariables = { input: GetVehicleInput }

type VehicleNotFound =
    { ///The name of the type
      __typename: string
      message: string }

/// State of a vehicle
type VehicleState =
    { ///The name of the type
      __typename: string
      vehicleId: string
      make: string
      model: string
      year: int
      status: string }

[<RequireQualifiedAccess>]
type GetVehicleResponse =
    | VehicleNotFound of vehiclenotfound: VehicleNotFound
    | VehicleState of vehiclestate: VehicleState

type Query =
    { /// Get the state of a vehicle
      getVehicle: GetVehicleResponse }
