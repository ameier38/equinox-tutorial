[<RequireQualifiedAccess>]
module rec PrivateApi.AddVehicle

type InputVariables = { input: AddVehicleInput }

type Query =
    { /// Add a new vehicle
      addVehicle: string }
