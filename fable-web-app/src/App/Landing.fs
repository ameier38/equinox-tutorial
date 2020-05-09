module Landing

open Elmish
open Feliz
open Feliz.Router
open Feliz.MaterialUI
open Graphql

let jumbotronImage = Image.load "./images/cosmos.jpg"
let rocketImage = Image.load "./images/rocket.svg"

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

let useStyles = Styles.makeStyles(fun styles theme ->
    {|
        jumbotron = styles.create [
            style.paddingTop 100
            style.paddingBottom 20
            style.backgroundImageUrl jumbotronImage
        ]
        jumbotronText = styles.create [
            style.color theme.palette.primary.contrastText
        ]
    |}
)


let jumbotron =
    React.functionComponent(fun _ ->
        let c = useStyles()
        let theme = Styles.useTheme()
        let isGteMd = Hooks.useMediaQuery(theme.breakpoints.upMd)
        Mui.paper [
            prop.className c.jumbotron
            paper.elevation 0
            paper.children [
                Mui.container [
                    container.maxWidth.md
                    container.children [
                        Mui.grid [
                            grid.container true
                            grid.justify.spaceBetween
                            grid.children [
                                Mui.grid [
                                    grid.item true
                                    grid.xs._12
                                    grid.md._6
                                    grid.children [
                                        Mui.typography [
                                            prop.className c.jumbotronText
                                            typography.variant.h2
                                            typography.children [
                                                "Best spaceship deals in the universe"
                                            ]
                                        ]
                                    ]
                                ]
                                if isGteMd then
                                    Mui.grid [
                                        grid.item true
                                        grid.md._3
                                        grid.children [
                                            Html.img [
                                                prop.src rocketImage
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    )

type VehiclesProps =
    { isLoading: bool
      vehicles: VehicleDto list
      dispatch: Msg -> unit }

let vehicles =
    React.functionComponent<VehiclesProps>(fun props ->
        let theme = Styles.useTheme()
        let isGteMd = Hooks.useMediaQuery(theme.breakpoints.upMd)
        Mui.container [
            container.disableGutters (not isGteMd)
            container.maxWidth.md
            container.children [
                Mui.grid [
                    grid.container true
                    grid.children [
                        if props.isLoading then
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
                        else
                            for vehicle in props.vehicles do
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
            ]
        ]
    )

let render (state:State) (dispatch:Msg -> unit) =
    React.fragment [
        jumbotron()
        match state.Vehicles with
        | NotStarted
        | InProgress ->
            vehicles { isLoading = true; vehicles = []; dispatch = dispatch }
        | Resolved result ->
            match result with
            | Ok res ->
                vehicles { isLoading = false; vehicles = res.vehicles; dispatch = dispatch }
            | Error error ->
                Log.error error
                Error.renderError()
    ]
