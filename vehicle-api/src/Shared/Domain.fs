namespace Shared

type Vehicle =
    { VehicleId: VehicleId
      Make: string
      Model: string
      Year: int }

type VehicleEvent =
    | VehicleAdded of Vehicle
    | VehicleRemoved of {| VehicleId: VehicleId |}
    | VehicleLeased of {| VehicleId: VehicleId |}
    | VehicleReturned of {| VehicleId: VehicleId |}
    interface TypeShape.UnionContract.IUnionContract

type VehicleCommand =
    | AddVehicle of Vehicle
    | RemoveVehicle
    | LeaseVehicle
    | ReturnVehicle

type VehicleStatus =
    | Unknown
    | Available
    | Removed
    | Leased

type VehicleState =
    { Vehicle: Vehicle option
      VehicleStatus: VehicleStatus }
