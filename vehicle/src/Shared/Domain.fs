namespace Shared

open System

type Vehicle =
    { Make: Make
      Model: Model
      Year: Year }

type VehicleEvent =
    | VehicleAdded of {| VehicleId: VehicleId; Vehicle: Vehicle |}
    | VehicleUpdated of {| VehicleId: VehicleId; Vehicle: Vehicle |}
    | AvatarUpdated of {| VehicleId: VehicleId; AvatarUri: Uri |}
    | AvatarRemoved of {| VehicleId: VehicleId |}
    | ImageAdded of {| VehicleId: VehicleId; ImageUri: Uri |}
    | ImageRemoved of {| VehicleId: VehicleId; ImageUri: Uri |}
    | VehicleRemoved of {| VehicleId: VehicleId |}
    | VehicleLeased of {| VehicleId: VehicleId |}
    | VehicleReturned of {| VehicleId: VehicleId |}
    interface TypeShape.UnionContract.IUnionContract

type VehicleCommand =
    | AddVehicle of Vehicle
    | UpdateVehicle of Vehicle
    | UpdateAvatar of Uri
    | RemoveAvatar
    | AddImage of Uri
    | RemoveImage of Uri
    | RemoveVehicle
    | LeaseVehicle
    | ReturnVehicle

type VehicleState =
    | Unknown
    | Available
    | Leased

type VehicleError =
    | VehicleNotFound of string
    | VehicleAlreadyExists of string
    | VehicleCurrentlyLeased of string
    | VehicleAlreadyReturned of string
