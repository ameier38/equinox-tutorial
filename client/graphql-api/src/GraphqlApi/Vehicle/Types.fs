module GraphqlApi.Vehicle.Types

open MongoDB.Bson
open GraphqlApi.Common.Types

type VehicleInput =
    { vehicleId: string
      make: string
      model: string
      year: int }

type UpdateVehicleAvatarInput =
    { vehicleId: string
      avatarUrl: string }

type RemoveVehicleAvatarInput =
    { vehicleId: string }

type AddVehicleImageInput =
    { vehicleId: string
      imageUrl: string }

type RemoveVehicleImageInput =
    { vehicleId: string
      imageUrl: string }

type RemoveVehicleInput =
    { vehicleId: string }

type Vehicle =
    { _id: ObjectId
      vehicleId: string
      make: string
      model: string
      year: int
      status: string }

type GetVehicleInput =
    { vehicleId: string }

[<RequireQualifiedAccess>]
type GetVehicleResponse =
    | Vehicle of Vehicle
    | VehicleNotFound of Message
    | PermissionDenied of Message

[<RequireQualifiedAccess>]
type GetAvailableVehicleResponse =
    | Vehicle of Vehicle
    | VehicleNotFound of Message

type Vehicles =
    { vehicles: Vehicle list
      totalCount: int64
      prevPageToken: string
      nextPageToken: string }

type ListVehiclesInput =
    { pageToken: string option
      pageSize: int option }

[<RequireQualifiedAccess>]
type ListVehiclesResponse =
    | Vehicles of Vehicles
    | PermissionDenied of Message
