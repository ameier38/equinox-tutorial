module Server.Vehicle.Types

open MongoDB.Bson

type ListVehiclesInput =
    { pageToken: string option
      pageSize: int option }

type GetVehicleInput =
    { vehicleId: string }

type AddVehicleInput =
    { vehicleId: string
      make: string
      model: string
      year: int }

type UpdateVehicleInput =
    { vehicleId: string
      make: string option
      model: string option
      year: int option }

type RemoveVehicleInput =
    { vehicleId: string }

type VehicleState =
    { _id: ObjectId
      vehicleId: string
      make: string
      model: string
      year: int
      status: string }

type VehicleNotFound =
    { message: string }

type GetVehicleResponse =
    | Found of VehicleState
    | NotFound of VehicleNotFound

type ListVehiclesResponse =
    { vehicles: VehicleState list
      totalCount: int64
      prevPageToken: string
      nextPageToken: string }
