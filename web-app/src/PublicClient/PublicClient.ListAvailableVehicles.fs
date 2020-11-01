[<RequireQualifiedAccess>]
module rec PublicClient.ListAvailableVehicles

type InputVariables = { input: ListVehiclesInput }

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
      addedAt: System.DateTime
      avatar: string
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
type ListAvailableVehiclesResponse =
    | PageTokenInvalid of pagetokeninvalid: PageTokenInvalid
    | PageSizeInvalid of pagesizeinvalid: PageSizeInvalid
    | ListVehiclesSuccess of listvehiclessuccess: ListVehiclesSuccess

type Query =
    { /// List available vehicles
      listAvailableVehicles: ListAvailableVehiclesResponse }
