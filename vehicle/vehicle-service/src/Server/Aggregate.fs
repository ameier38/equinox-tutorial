module Server.Aggregate

open Shared

let initial =
    { Vehicle = None
      VehicleStatus = Unknown }

let evolve : VehicleState -> VehicleEvent -> VehicleState =
    fun state event ->
        match event with
        | VehicleAdded vehicle ->
            { Vehicle = Some vehicle
              VehicleStatus = Available }
        | VehicleRemoved _ ->
            { state with
                VehicleStatus = Removed }
        | VehicleLeased _ ->
            { state with
                VehicleStatus = Leased }
        | VehicleReturned _ ->
            { state with
                VehicleStatus = Available }

let interpret
    (vehicleId:VehicleId)
    : VehicleCommand -> VehicleState -> VehicleEvent list =
    fun command state ->
        let vehicleIdStr = Guid.toStringN vehicleId
        match command with
        | AddVehicle vehicle ->
            match state.VehicleStatus with
            | Unknown ->
                [VehicleAdded vehicle]
            | other -> 
                sprintf "cannot add vehicle; Vehicle-%s already exists: %A" vehicleIdStr other
                |> RpcException.raiseAlreadyExists
        | RemoveVehicle ->
            match state.VehicleStatus with
            | Available ->
                [VehicleRemoved {| VehicleId = vehicleId |}]
            | Removed ->
                sprintf "cannot remove vehicle; Vehicle-%s already removed" vehicleIdStr
                |> RpcException.raiseInternal
            | Leased ->
                sprintf "cannot remove vehicle; Vehicle-%s is leased" vehicleIdStr
                |> RpcException.raiseInternal
            | Unknown ->
                sprintf "cannot remove vehicle; Vehicle-%s does not exist" vehicleIdStr
                |> RpcException.raiseNotFound
        | LeaseVehicle ->
            match state.VehicleStatus with
            | Available ->
                [VehicleLeased {| VehicleId = vehicleId |}]
            | Removed ->
                sprintf "cannot lease vehicle; Vehicle-%s removed" vehicleIdStr
                |> RpcException.raiseInternal
            | Leased ->
                sprintf "cannot lease vehicle; Vehicle-%s already leased" vehicleIdStr
                |> RpcException.raiseInternal
            | Unknown ->
                sprintf "cannot lease vehicle; Vehicle-%s does not exist" vehicleIdStr
                |> RpcException.raiseNotFound
        | ReturnVehicle ->
            match state.VehicleStatus with
            | Leased ->
                [VehicleReturned {| VehicleId = vehicleId |}]
            | Available ->
                sprintf "cannot return vehicle; Vehicle-%s already returned" vehicleIdStr
                |> RpcException.raiseInternal
            | Removed ->
                sprintf "cannot return vehicle; Vehicle-%s is removed" vehicleIdStr
                |> RpcException.raiseInternal
            | Unknown ->
                sprintf "cannot return vehicle; Vehicle-%s does not exist" vehicleIdStr
                |> RpcException.raiseNotFound

let fold: VehicleState -> seq<VehicleEvent> -> VehicleState =
    Seq.fold evolve
