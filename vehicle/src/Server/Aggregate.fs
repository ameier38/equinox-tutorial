module Server.Aggregate

open Shared

let initial = Unknown

let evolve : VehicleState -> VehicleEvent -> VehicleState =
    fun state event ->
        match event with
        | VehicleUpdated _
        | ImageAdded _
        | ImageRemoved _
        | AvatarUpdated _
        | AvatarRemoved _ -> state
        | VehicleAdded _ -> Available
        | VehicleRemoved _ -> Unknown
        | VehicleLeased _ -> Leased
        | VehicleReturned _ -> Available

let decide
    (vehicleId:VehicleId)
    : VehicleCommand -> VehicleState -> Result<string, VehicleError> * VehicleEvent list =
    fun command state ->
        let vehicleIdStr = VehicleId.toStringN vehicleId
        match command with
        | AddVehicle vehicle ->
            match state with
            | Unknown ->
                Ok (sprintf "successfully added Vehicle-%s" vehicleIdStr),
                [VehicleAdded {| VehicleId = vehicleId; Vehicle = vehicle |}]
            | other -> 
                let error =
                    sprintf "cannot add vehicle; Vehicle-%s already exists: %A" vehicleIdStr other
                    |> VehicleAlreadyExists
                    |> Error
                error, []
        | UpdateVehicle vehicle ->
            match state with
            | Available
            | Leased ->
                Ok (sprintf "successfully updated Vehicle-%s" vehicleIdStr),
                [VehicleUpdated {| VehicleId = vehicleId; Vehicle = vehicle |}]
            | Unknown ->
                let error =
                    sprintf "cannot update vehicle; Vehicle-%s does not exist" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | UpdateAvatar imageUri ->
            match state with
            | Available
            | Leased ->
                Ok (sprintf "successfully updated Vehicle-%s avatar" vehicleIdStr),
                [AvatarUpdated {| VehicleId = vehicleId; AvatarUri = imageUri |}]
            | Unknown ->
                let error =
                    sprintf "cannot update vehicle avatar; Vehicle-%s does not exist" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | RemoveAvatar ->
            match state with
            | Available
            | Leased ->
                Ok (sprintf "successfully removed Vehicle-%s avatar" vehicleIdStr),
                [AvatarRemoved {| VehicleId = vehicleId |}]
            | Unknown ->
                let error =
                    sprintf "cannot remove vehicle avatar; Vehicle-%s does not exist" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | AddImage imageUri ->
            match state with
            | Available
            | Leased ->
                Ok (sprintf "successfully added Vehicle-%s image" vehicleIdStr),
                [ImageAdded {| VehicleId = vehicleId; ImageUri = imageUri |}]
            | Unknown ->
                let error =
                    sprintf "cannot add vehicle image; Vehicle-%s does not exist" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | RemoveImage imageUri ->
            match state with
            | Available
            | Leased ->
                Ok (sprintf "successfully removed Vehicle-%s image" vehicleIdStr),
                [ImageRemoved {| VehicleId = vehicleId; ImageUri = imageUri |}]
            | Unknown ->
                let error =
                    sprintf "cannot remove vehicle image; Vehicle-%s does not exist" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | RemoveVehicle ->
            match state with
            | Available ->
                Ok (sprintf "successfully removed Vehicle-%s" vehicleIdStr),
                [ VehicleRemoved {| VehicleId = vehicleId |} ]
            | Leased ->
                let error =
                    sprintf "cannot remove vehicle; Vehicle-%s is leased" vehicleIdStr
                    |> VehicleCurrentlyLeased
                    |> Error
                error, []
            | Unknown ->
                let error =
                    sprintf "cannot remove vehicle; Vehicle-%s does not exist" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | LeaseVehicle ->
            match state with
            | Available ->
                Ok (sprintf "successfully leased Vehicle-%s" vehicleIdStr),
                [VehicleLeased {| VehicleId = vehicleId |}]
            | Leased _ ->
                let error =
                    sprintf "cannot lease vehicle; Vehicle-%s currently leased" vehicleIdStr
                    |> VehicleCurrentlyLeased
                    |> Error
                error, []
            | Unknown ->
                let error =
                    sprintf "cannot lease vehicle; Vehicle-%s does not exist" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | ReturnVehicle ->
            match state with
            | Leased ->
                Ok (sprintf "successfully returned Vehicle-%s" vehicleIdStr),
                [ VehicleReturned {| VehicleId = vehicleId |} ]
            | Available _ ->
                let error =
                    sprintf "cannot return vehicle; Vehicle-%s already returned" vehicleIdStr
                    |> VehicleAlreadyReturned
                    |> Error
                error, []
            | Unknown ->
                let error =
                    sprintf "cannot return vehicle; Vehicle-%s does not exist" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []

let fold: VehicleState -> seq<VehicleEvent> -> VehicleState =
    Seq.fold evolve
