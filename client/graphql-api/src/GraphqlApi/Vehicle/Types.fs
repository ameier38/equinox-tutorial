module GraphqlApi.Vehicle.Types

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

type GetVehicleInput =
    { vehicleId: string }

type ListVehiclesInput =
    { pageToken: string option
      pageSize: int option }
