namespace Server

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

type VehicleStateDto =
    { _id: ObjectId
      vehicleId: string
      make: string
      model: string
      year: int
      status: string }

type ListVehiclesResponseDto =
    { vehicles: VehicleStateDto list
      totalCount: int64
      prevPageToken: string
      nextPageToken: string }
