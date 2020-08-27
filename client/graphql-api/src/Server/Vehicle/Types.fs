module Server.Vehicle.Types

open FSharp.Data.GraphQL.Types
open MongoDB.Bson
open Server.Common.Types

type AddVehicleInput =
    { vehicleId: string
      make: string
      model: string
      year: int }

[<RequireQualifiedAccess>]
type AddVehicleResponse =
    | Success of Message
    | AlreadyExists of Message
    | PermissionDenied of Message

type UpdateVehicleInput =
    { vehicleId: string
      make: string option
      model: string option
      year: int option }

[<RequireQualifiedAccess>]
type UpdateVehicleResponse =
    | Success of Message
    | NotFound of Message
    | PermissionDenied of Message

type RemoveVehicleInput =
    { vehicleId: string }

[<RequireQualifiedAccess>]
type RemoveVehicleResponse =
    | Success of Message
    | NotFound of Message
    | PermissionDenied of Message

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
    | Data of Vehicle
    | NotFound of Message
    | PermissionDenied of Message

[<RequireQualifiedAccess>]
type GetAvailableVehicleResponse =
    | Data of Vehicle
    | NotFound of Message

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
    | Data of Vehicles
    | PermissionDenied of Message
