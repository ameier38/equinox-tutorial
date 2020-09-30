[<RequireQualifiedAccess>]
module rec PrivateClient.GetVehicle

type InputVariables = { input: GetVehicleInput }

type VehicleNotFound =
    { ///The name of the type
      __typename: string
      message: string }

type PermissionDenied =
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
      addedAt: System.DateTime
      updatedAt: System.DateTime
      status: VehicleStatus
      vehicle: Vehicle
      avatar: string
      images: list<string> }

[<RequireQualifiedAccess>]
type GetVehicleResponse =
    | VehicleNotFound of vehiclenotfound: VehicleNotFound
    | PermissionDenied of permissiondenied: PermissionDenied
    | InventoriedVehicle of inventoriedvehicle: InventoriedVehicle

type Query =
    { /// Get the state of a vehicle
      getVehicle: GetVehicleResponse }
