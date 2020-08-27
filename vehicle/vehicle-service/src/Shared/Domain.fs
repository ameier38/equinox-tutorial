namespace Shared

open System

type Vehicle =
    { VehicleId: VehicleId
      Make: string
      Model: string
      Year: int
      AvatarUrl: Uri
      ImageUrls: Uri list }

type VehicleUpdates =
    { Make: string option
      Model: string option
      Year: int option
      AvatarUrl: Uri option }

type VehicleEvent =
    | VehicleAdded of Vehicle
    | VehicleUpdated of Vehicle
    | VehicleImageAdded of Vehicle
    | VehicleRemoved of {| VehicleId: VehicleId |}
    | VehicleLeased of {| VehicleId: VehicleId |}
    | VehicleReturned of {| VehicleId: VehicleId |}
    interface TypeShape.UnionContract.IUnionContract

type VehicleCommand =
    | AddVehicle of Vehicle
    | UpdateVehicle of VehicleUpdates
    | AddVehicleImage of Uri
    | RemoveVehicle
    | LeaseVehicle
    | ReturnVehicle

type VehicleState =
    | Unknown
    | Available of Vehicle
    | Leased of Vehicle
    | Removed
