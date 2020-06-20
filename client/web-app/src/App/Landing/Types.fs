module Landing.Types

open Snowflaqe

type State =
    { Vehicles: ListVehicles.VehicleState list
      TotalCount: int
      PrevPageToken: string
      NextPageToken: string }

type Msg =
    | NavigateToVehicle of VehicleId
