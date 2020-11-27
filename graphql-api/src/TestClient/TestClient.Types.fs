namespace rec TestClient

///Status of a vehicle
[<Fable.Core.StringEnum; RequireQualifiedAccess>]
type VehicleStatus =
    | [<CompiledName "Unknown">] Unknown
    | [<CompiledName "Available">] Available
    | [<CompiledName "Leased">] Leased

type GetVehicleInput =
    { /// Unique identifier for the vehicle
      vehicleId: string }

type ListVehiclesInput =
    { /// Token for page to retrieve
      pageToken: Option<string>
      /// Maximum number of items in a page
      pageSize: Option<int> }

type AddVehicleInput =
    { /// Unique identifier for the vehicle
      vehicleId: string
      make: string
      model: string
      year: int }

type AddVehicleImageInput =
    { /// Unique identifier for the vehicle
      vehicleId: string
      imageUrl: string }

type RemoveVehicleInput =
    { /// Unique identifier for the vehicle
      vehicleId: string }

type RemoveVehicleAvatarInput =
    { /// Unique identifier for the vehicle
      vehicleId: string }

type RemoveVehicleImageInput =
    { /// Unique identifier for the vehicle
      vehicleId: string
      imageUrl: string }

type UpdateVehicleInput =
    { /// Unique identifier for the vehicle
      vehicleId: string
      make: string
      model: string
      year: int }

type UpdateVehicleAvatarInput =
    { /// Unique identifier for the vehicle
      vehicleId: string
      avatarUrl: string }

/// The error returned by the GraphQL backend
type ErrorType = { message: string }
