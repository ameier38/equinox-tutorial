module Server.Vehicle.Types

open MongoDB.Bson

type ListVehiclesInput =
    { pageToken: string option
      pageSize: int option }

type GetVehicleInput =
    { vehicleId: string }

type AddVehicleInput =
    { make: string
      model: string
      year: int }

type VehicleState =
    { _id: ObjectId
      vehicleId: string
      make: string
      model: string
      year: int
      status: string }

type ListVehiclesResponse =
    { vehicles: VehicleState list
      totalCount: int64
      prevPageToken: string
      nextPageToken: string }
