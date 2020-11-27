[<RequireQualifiedAccess>]
module rec TestClient.ListVehicles

type InputVariables = { input: ListVehiclesInput }

type PermissionDenied =
    { ///The name of the type
      __typename: string
      message: string }

type PageTokenInvalid =
    { ///The name of the type
      __typename: string
      message: string }

type PageSizeInvalid =
    { ///The name of the type
      __typename: string
      message: string }

type Vehicle =
    { make: string
      model: string
      year: int }

type InventoriedVehicle =
    { vehicleId: string
      status: VehicleStatus
      vehicle: Vehicle }

/// Successful list vehicles response
type ListVehiclesSuccess =
    { ///The name of the type
      __typename: string
      totalCount: int
      nextPageToken: string
      vehicles: list<InventoriedVehicle> }

[<RequireQualifiedAccess>]
type ListVehiclesResponse =
    | PermissionDenied of permissiondenied: PermissionDenied
    | PageTokenInvalid of pagetokeninvalid: PageTokenInvalid
    | PageSizeInvalid of pagesizeinvalid: PageSizeInvalid
    | ListVehiclesSuccess of listvehiclessuccess: ListVehiclesSuccess

type Query =
    { /// List all vehicles
      listVehicles: ListVehiclesResponse }
