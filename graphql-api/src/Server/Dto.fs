namespace Server

type ListVehiclesInput =
    { PageToken: string option
      PageSize: int option }

type GetVehicleInput =
    { VehicleId: string }

type AddVehicleInput =
    { Make: string
      Model: string
      Year: int }
