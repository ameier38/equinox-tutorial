namespace Vehicle

type Vehicle =
    { Make: string
      Model: string
      Year: int }

type VehicleEvent =
    | VehicleAdded of Vehicle
    | VehicleRemoved
    | VehicleLeased
    interface TypeShape.UnionContract.IUnionContract

type VehicleCommand =
    | AddVehicle of Vehicle
    | RemoveVehicle
    | LeaseVehicle

type VehicleStatus =
    | Unknown
    | Available
    | Removed
    | Leased

type VehicleState =
    { Vehicle: Vehicle option
      VehicleStatus: VehicleStatus }
