module Landing.State

open Elmish
open FSharp.UMX
open Snowflaqe

let init (): unit * Cmd<Msg> =
    let pageToken = UMX.tag<pageToken> ""

    (), Cmd.none

let update (msg:Msg) (state:unit): unit * Cmd<Msg> =
    match msg with
    | NavigateToVehicle vehicleId ->
        state, Router.navigate (sprintf "vehicles/%s" (UMX.untag vehicleId))
