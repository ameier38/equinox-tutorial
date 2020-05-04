module Landing

open Elmish
open Feliz
open Feliz.Router
open Feliz.MaterialUI
open Graphql

type State =
    { NextPageToken: PageToken
      Vehicles: Deferred<Result<ListVehiclesResponseDto,string>> }

type Msg =
    | ListVehicles of AsyncOperation<PageToken,Result<ListVehiclesResponseDto,string>>
    | NavigateToVehicle of VehicleId

let listVehicles (input:ListVehiclesInputDto): Cmd<Msg> =
    async {
        let! response = graphqlClient.ListVehicles(input)
        return ListVehicles (Finished response)
    } |> Cmd.fromAsync

let init (): State * Cmd<Msg> =
    let pageToken = PageToken.fromString ""
    let state =
        { NextPageToken = pageToken
          Vehicles = NotStarted }
    state, Cmd.ofMsg(ListVehicles (Started pageToken))

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | NavigateToVehicle vehicleId ->
        state, Router.navigate (sprintf "vehicles/%s" (VehicleId.toString vehicleId))
    | ListVehicles (Started pageToken) ->
        let input =
            { pageToken = PageToken.toString pageToken
              pageSize = 10 }
        let newState =
            { state with
                  Vehicles = InProgress }
        newState, listVehicles input
    | ListVehicles (Finished result) ->
        let newState =
            { state with
                Vehicles = Resolved result }
        newState, Cmd.none

let renderVehicles (vehicles:VehicleDto list) (dispatch:Msg -> unit) =
    Mui.grid [
        grid.container true
        grid.spacing._2
        grid.children [
            for vehicle in vehicles do
                let makeModel = sprintf "%s %s" vehicle.make vehicle.model
                yield Mui.grid [
                    prop.key vehicle.vehicleId
                    grid.item true
                    grid.xs._12
                    grid.md._4
                    grid.children [
                        Mui.card [
                            card.children [
                                Mui.cardContent [
                                    Mui.typography [
                                        typography.variant.h6
                                        prop.text makeModel
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

let renderSkeleton () =
    Mui.grid [
        grid.container true
        grid.spacing._2
        grid.children [
            for i in 1..5 do
                yield Mui.grid [
                    prop.key i
                    grid.item true
                    grid.xs._12
                    grid.md._4
                    grid.children [
                        Mui.card [
                            card.children [
                                Mui.cardContent [
                                    Mui.skeleton [
                                        skeleton.animation.wave
                                        skeleton.width (length.perc 100)
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

let render (state:State) (dispatch:Msg -> unit) =
    match state.Vehicles with
    | NotStarted
    | InProgress ->
        renderSkeleton()
    | Resolved result ->
        match result with
        | Ok res ->
            renderVehicles res.vehicles dispatch
        | Error error ->
            Error.renderError error
