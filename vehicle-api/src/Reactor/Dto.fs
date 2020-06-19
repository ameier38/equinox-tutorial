namespace Reactor

open Shared

type CheckpointDto =
    { model: string
      checkpoint: int64 }

type VehicleDto =
    { vehicleId: string
      make: string
      model: string
      year: int
      status: string }

module VehicleStatus =
    let toString =
        function
        | Unknown -> "Unknown"
        | Available -> "Available"
        | Removed -> "Removed"
        | Leased -> "Leased"
