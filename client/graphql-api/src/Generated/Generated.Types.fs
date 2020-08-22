namespace rec Generated

type GetVehicleInput =
    { /// Unique identifier for the vehicle
      vehicleId: string }

type ListVehiclesInput =
    { /// Token for page to retrieve; Empty string for first page
      pageToken: Option<string>
      /// Maximum number of items in a page
      pageSize: Option<int> }

type AddVehicleInput =
    { /// Unique identifier for the vehicle
      vehicleId: string
      make: string
      model: string
      year: int }

type RemoveVehicleInput =
    { /// Unique identifier for the vehicle
      vehicleId: string }

type UpdateVehicleInput =
    { /// Unique identifier for the vehicle
      vehicleId: string
      make: Option<string>
      model: Option<string>
      year: Option<int> }

/// The error returned by the GraphQL backend
type ErrorType = { message: string }
