module Server.Aggregate

open Shared

let initial = Unknown

let evolve : VehicleState -> VehicleEvent -> VehicleState =
    fun state event ->
        match event with
        | VehicleAdded vehicle ->
            Available vehicle
        | VehicleUpdated vehicle ->
            match state with
            | Available _ -> Available vehicle
            | Leased _ -> Leased vehicle
            | other -> other
        | VehicleRemoved _ ->
            Removed
        | VehicleLeased _ ->
            match state with
            | Available vehicle -> Leased vehicle
            | other -> other
        | VehicleReturned _ ->
            match state with
            | Leased vehicle -> Available vehicle
            | other -> other

let interpret
    (vehicleId:VehicleId)
    : VehicleCommand -> VehicleState -> VehicleEvent list =
    fun command state ->
        let vehicleIdStr = Guid.toStringN vehicleId
        match command with
        | AddVehicle vehicle ->
            match state with
            | Unknown ->
                [VehicleAdded vehicle]
            | other -> 
                sprintf "cannot add vehicle; Vehicle-%s already exists: %A" vehicleIdStr other
                |> RpcException.raiseAlreadyExists
        | UpdateVehicle updates ->
            match state with
            | Available vehicle
            | Leased vehicle ->
                let newVehicle = 
                    { vehicle with
                        Make = updates.Make |> Option.defaultValue vehicle.Make
                        Model = updates.Model |> Option.defaultValue vehicle.Model
                        Year = updates.Year |> Option.defaultValue vehicle.Year }
                [VehicleUpdated newVehicle]
            | Removed ->
                sprintf "cannot update vehicle; Vehicle-%s already removed" vehicleIdStr
                |> RpcException.raiseInternal
            | Unknown ->
                sprintf "cannot update vehicle; Vehicle-%s does not exist" vehicleIdStr
                |> RpcException.raiseNotFound
        | RemoveVehicle ->
            match state with
            | Available vehicle ->
                [VehicleRemoved {| VehicleId = vehicle.VehicleId |}]
            | Removed ->
                sprintf "cannot remove vehicle; Vehicle-%s already removed" vehicleIdStr
                |> RpcException.raiseInternal
            | Leased _ ->
                sprintf "cannot remove vehicle; Vehicle-%s is leased" vehicleIdStr
                |> RpcException.raiseInternal
            | Unknown ->
                sprintf "cannot remove vehicle; Vehicle-%s does not exist" vehicleIdStr
                |> RpcException.raiseNotFound
        | LeaseVehicle ->
            match state with
            | Available vehicle ->
                [VehicleLeased {| VehicleId = vehicle.VehicleId |}]
            | Removed ->
                sprintf "cannot lease vehicle; Vehicle-%s removed" vehicleIdStr
                |> RpcException.raiseInternal
            | Leased _ ->
                sprintf "cannot lease vehicle; Vehicle-%s already leased" vehicleIdStr
                |> RpcException.raiseInternal
            | Unknown ->
                sprintf "cannot lease vehicle; Vehicle-%s does not exist" vehicleIdStr
                |> RpcException.raiseNotFound
        | ReturnVehicle ->
            match state with
            | Leased vehicle ->
                [VehicleReturned {| VehicleId = vehicle.VehicleId |}]
            | Available _ ->
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
