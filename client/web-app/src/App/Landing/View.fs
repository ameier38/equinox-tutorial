module Landing.View

open Elmish
open Feliz
open Feliz.Router
open Feliz.MaterialUI
open Feliz.UseDeferred
open FSharp.UMX
open Snowflaqe
open Types

let jumbotronImage = Image.load "./images/cosmos.jpg"
let rocketImage = Image.load "./images/rocket.svg"

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

let vehicles =
    React.functionComponent(fun _ ->
        let (pageToken, setPageToken) = React.useState("")
        let (pageSize, setPageSize) = React.useState(10)
        let input = { pageToken = Some pageToken; pageSize = Some pageSize }
        let listVehicles =
            async {
                List
            }
        let data = React.useDeferred()
        let theme = Styles.useTheme()
        let isGteMd = Hooks.useMediaQuery(theme.breakpoints.upMd)
        Mui.container [
            container.disableGutters (not isGteMd)
            container.maxWidth.md
            container.children [
                Mui.grid [
                    grid.container true
                    grid.children [
                        if res.error.IsSome then
                            yield Mui.grid [
                                grid.item true
                                grid.xs._12
                                grid.children [
                                    Error.renderError()
                                ]
                            ]
                        elif res.loading then
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
                            Log.debug ("vehicles", res.data)
                            for vehicle in res.data.vehicles do
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

let render (state:unit) (dispatch:Msg -> unit) =
    React.fragment [
        jumbotron()
        vehicles()
    ]