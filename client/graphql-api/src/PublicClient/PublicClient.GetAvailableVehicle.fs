[<RequireQualifiedAccess>]
module rec PublicClient.GetAvailableVehicle

type InputVariables = { input: GetVehicleInput }

type VehicleNotFound =
    { ///The name of the type
      __typename: string
      message: string }

type Vehicle =
    { make: string
      model: string
      year: int }

/// A vehicle in inventory
type InventoriedVehicle =
    { ///The name of the type
      __typename: string
      vehicleId: string
      status: VehicleStatus
      vehicle: Vehicle }

[<RequireQualifiedAccess>]
type GetAvailableVehicleResponse =
    | VehicleNotFound of vehiclenotfound: VehicleNotFound
    | InventoriedVehicle of inventoriedvehicle: InventoriedVehicle

type Query =
    { /// Get the state of an available vehicle
      getAvailableVehicle: GetAvailableVehicleResponse }
