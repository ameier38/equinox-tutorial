namespace rec PublicClient

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

/// The error returned by the GraphQL backend
type ErrorType = { message: string }
