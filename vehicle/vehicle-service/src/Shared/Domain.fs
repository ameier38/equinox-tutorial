namespace Shared

type Vehicle =
    { VehicleId: VehicleId
      Make: string
      Model: string
      Year: int }

type VehicleUpdates =
    { Make: string option
      Model: string option
      Year: int option }

type VehicleEvent =
    | VehicleAdded of Vehicle
    | VehicleUpdated of Vehicle
    | VehicleRemoved of {| VehicleId: VehicleId |}
    | VehicleLeased of {| VehicleId: VehicleId |}
    | VehicleReturned of {| VehicleId: VehicleId |}
    interface TypeShape.UnionContract.IUnionContract

type VehicleCommand =
    | AddVehicle of Vehicle
    | UpdateVehicle of VehicleUpdates
    | RemoveVehicle
    | LeaseVehicle
    | ReturnVehicle

type VehicleState =
    | Unknown
    | Available of Vehicle
    | Leased of Vehicle
    | Removed
