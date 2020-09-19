namespace Shared

type Vehicle =
    { Make: Make
      Model: Model
      Year: Year }

type VehicleCommand =
    | AddVehicle of Vehicle
    | UpdateVehicle of Vehicle
    | UpdateAvatar of Url
    | RemoveAvatar
    | AddImage of Url
    | RemoveImage of Url
    | RemoveVehicle
    | LeaseVehicle
    | ReturnVehicle

type VehicleEvent =
    | VehicleAdded of {| VehicleId: VehicleId; Vehicle: Vehicle |}
    | VehicleUpdated of {| VehicleId: VehicleId; Vehicle: Vehicle |}
    | AvatarUpdated of {| VehicleId: VehicleId; AvatarUrl: Url |}
    | AvatarRemoved of {| VehicleId: VehicleId |}
    | ImageAdded of {| VehicleId: VehicleId; ImageUrl: Url |}
    | ImageRemoved of {| VehicleId: VehicleId; ImageUrl: Url |}
    | VehicleRemoved of {| VehicleId: VehicleId |}
    | VehicleLeased of {| VehicleId: VehicleId |}
    | VehicleReturned of {| VehicleId: VehicleId |}
    interface TypeShape.UnionContract.IUnionContract

type VehicleStatus =
    | Unknown
    | Available
    | Leased

type VehicleState =
    { VehicleStatus: VehicleStatus
      AvatarUrl: Url
      ImageUrls: Url list }

type VehicleError =
    | VehicleNotFound of string
    | VehicleAlreadyExists of string
    | VehicleCurrentlyLeased of string
    | VehicleAlreadyReturned of string
    | ImageNotFound of string
    | MaxImageCountReached of string
