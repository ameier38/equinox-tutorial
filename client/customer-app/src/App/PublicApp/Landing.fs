module PublicApp.Landing

open Elmish
open Feliz
open Feliz.MaterialUI
open Feliz.Router
open Feliz.UseDeferred
open GraphQL
open PublicApi

let jumbotronImage = Image.load "../images/cosmos.jpg"
let rocketImage = Image.load "../images/rocket.svg"

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
        item = styles.create [
            style.padding 10
        ]
        clickable = styles.create [
            style.cursor.pointer
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
        let c = useStyles()
        let theme = Styles.useTheme()
        let isGteMd = Hooks.useMediaQuery(theme.breakpoints.upMd)
        let (pageToken, setPageToken) = React.useState("")
        let (pageSize, setPageSize) = React.useState(10)
        let { publicApi = gql } = React.useGQL()
        let input = { pageToken = Some pageToken; pageSize = Some pageSize }
        let data = React.useDeferred(gql.ListAvailableVehicles { input = input }, [||])
        Mui.container [
            container.disableGutters (not isGteMd)
            container.maxWidth.md
            container.children [
                Mui.grid [
                    grid.container true
                    grid.children [
                        match data with
                        | Deferred.Failed error ->
                            Log.error error
                            yield Mui.grid [
                                grid.item true
                                grid.xs._12
                                grid.children [
                                    Error.renderError()
                                ]
                            ]
                        | Deferred.Resolved (Error errors) ->
                            Log.error errors
                            yield Mui.grid [
                                grid.item true
                                grid.xs._12
                                grid.children [
                                    Error.renderError()
                                ]
                            ]
                        | Deferred.HasNotStartedYet
                        | Deferred.InProgress ->
                            for i in 1..5 do
                                yield Mui.grid [
                                    prop.key i
                                    grid.item true
                                    grid.xs._12
                                    grid.md._4
                                    grid.children [
                                        Mui.card [
                                            prop.className c.item
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
                        | Deferred.Resolved (Ok { listAvailableVehicles = res }) ->
                            Log.debug ("vehicles", res.vehicles)
                            for vehicle in res.vehicles do
                                let makeModel = sprintf "%s %s" vehicle.make vehicle.model
                                yield Mui.grid [
                                    prop.className c.item
                                    prop.key vehicle.vehicleId
                                    grid.item true
                                    grid.xs._12
                                    grid.md._4
                                    grid.children [
                                        Mui.card [
                                            prop.className c.clickable
                                            prop.onClick (fun evt ->
                                                evt.preventDefault()
                                                Router.navigatePath("vehicles", vehicle.vehicleId)
                                            )
                                            card.children [
                                                Mui.cardContent [
                                                    Mui.typography [
                                                        typography.variant.h6
                                                        prop.text makeModel
                                                    ]
                                                    Mui.typography [
                                                        typography.variant.body2
                                                        prop.text (sprintf "This vehicle is %s" vehicle.status)
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

let render () =
    React.fragment [
        jumbotron()
        vehicles()
    ]
