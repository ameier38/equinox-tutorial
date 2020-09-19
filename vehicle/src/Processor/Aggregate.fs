module Server.Aggregate

open FSharp.UMX
open Shared

let initial =
    { VehicleStatus = Unknown
      AvatarUrl = Url.empty
      ImageUrls = [] }

let evolve : VehicleState -> VehicleEvent -> VehicleState =
    fun state event ->
        match event with
        | VehicleUpdated _ -> state
        | AvatarUpdated payload -> 
            { state with AvatarUrl = payload.AvatarUrl }
        | AvatarRemoved _ ->
            { state with AvatarUrl = Url.empty }
        | ImageAdded payload ->
            { state with ImageUrls = payload.ImageUrl :: state.ImageUrls }
        | ImageRemoved payload ->
            { state with ImageUrls = state.ImageUrls |> List.filter ((<>) payload.ImageUrl) }
        | VehicleAdded _ ->
            { state with VehicleStatus = Available }
        | VehicleRemoved _ ->
            { state with VehicleStatus = Unknown }
        | VehicleLeased _ ->
            { state with VehicleStatus = Leased }
        | VehicleReturned _ ->
            { state with VehicleStatus = Available }

let decide
    (vehicleId:VehicleId)
    : VehicleCommand -> VehicleState -> Result<string, VehicleError> * VehicleEvent list =
    fun command state ->
        let vehicleIdStr = VehicleId.toStringN vehicleId
        match command with
        | AddVehicle vehicle ->
            match state.VehicleStatus with
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
            match state.VehicleStatus with
            | Available
            | Leased ->
                Ok (sprintf "successfully updated Vehicle-%s" vehicleIdStr),
                [VehicleUpdated {| VehicleId = vehicleId; Vehicle = vehicle |}]
            | Unknown ->
                let error =
                    sprintf "cannot update vehicle; Vehicle-%s not found" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | UpdateAvatar avatarUrl ->
            match state.VehicleStatus with
            | Available
            | Leased ->
                Ok (sprintf "successfully updated Vehicle-%s avatar" vehicleIdStr),
                [AvatarUpdated {| VehicleId = vehicleId; AvatarUrl = avatarUrl |}]
            | Unknown ->
                let error =
                    sprintf "cannot update avatar; Vehicle-%s not found" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | RemoveAvatar ->
            match state.VehicleStatus with
            | Available
            | Leased ->
                Ok (sprintf "successfully removed Vehicle-%s avatar" vehicleIdStr),
                [AvatarRemoved {| VehicleId = vehicleId |}]
            | Unknown ->
                let error =
                    sprintf "cannot remove avatar; Vehicle-%s not found" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | AddImage imageUrl ->
            match state.VehicleStatus with
            | Available
            | Leased ->
                if state.ImageUrls |> List.length < 10 then
                    Ok (sprintf "successfully added Vehicle-%s image" vehicleIdStr),
                    [ImageAdded {| VehicleId = vehicleId; ImageUrl = imageUrl |}]
                else
                    let error =
                        sprintf "cannot add image %s; max image count reached" %imageUrl
                        |> MaxImageCountReached
                        |> Error
                    error, []
            | Unknown ->
                let error =
                    sprintf "cannot add image; Vehicle-%s not found" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | RemoveImage imageUrl ->
            match state.VehicleStatus with
            | Available
            | Leased ->
                if state.ImageUrls |> List.contains imageUrl then
                    Ok (sprintf "successfully removed Vehicle-%s image" vehicleIdStr),
                    [ImageRemoved {| VehicleId = vehicleId; ImageUrl = imageUrl |}]
                else
                    let error =
                        sprintf "cannot remove image; %s not found" %imageUrl 
                        |> ImageNotFound
                        |> Error
                    error, []
            | Unknown ->
                let error =
                    sprintf "cannot remove image; Vehicle-%s not found" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | RemoveVehicle ->
            match state.VehicleStatus with
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
                    sprintf "cannot remove vehicle; Vehicle-%s not found" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | LeaseVehicle ->
            match state.VehicleStatus with
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
                    sprintf "cannot lease vehicle; Vehicle-%s not found" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []
        | ReturnVehicle ->
            match state.VehicleStatus with
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
                    sprintf "cannot return vehicle; Vehicle-%s not found" vehicleIdStr
                    |> VehicleNotFound
                    |> Error
                error, []

let fold: VehicleState -> seq<VehicleEvent> -> VehicleState =
    Seq.fold evolve
