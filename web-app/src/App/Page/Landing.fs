module Page.Landing

open Elmish
open Feliz
open Feliz.MaterialUI
open Feliz.UseDeferred
open GraphQL
open PublicClient

let jumbotronImage = Image.load "../images/cosmos.jpg"
let rocketImage = Image.load "../images/rocket.svg"

let useStyles = Styles.makeStyles(fun styles theme ->
    {|
        jumbotron = styles.create [
            style.paddingBottom 20
            style.backgroundImageUrl jumbotronImage
            style.marginBottom 20
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
            paper.square true
            paper.children [
                Common.PageContainer.render true [
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
    )

let vehicles =
    React.functionComponent<unit>(fun _ ->
        let { publicClient = gql } = React.useGQL()
        let input = { pageToken = None; pageSize = Some 10 }
        let data = React.useDeferred(gql.ListAvailableVehicles { input = input }, [||])
        Common.PageContainer.render false [
            match data with
            | Deferred.HasNotStartedYet
            | Deferred.InProgress ->
                let cards = [ for i in 0..5 -> Common.Grid.skeletonCard() ]
                Common.Grid.gridContainer ({| cards = cards |})
            | Deferred.Failed err ->
                Html.h1 (sprintf "Error!: %A" err)
            | Deferred.Resolved (Error errors) ->
                Html.h1 (sprintf "Error!: %A" errors)
            | Deferred.Resolved (Ok { listAvailableVehicles = res }) ->
                match res with
                | ListAvailableVehicles.ListAvailableVehiclesResponse.ListVehiclesSuccess success ->
                    let cards =
                        [ for vehicle in success.vehicles ->
                            Common.Grid.vehicleCard
                                ({ vehicleId = vehicle.vehicleId
                                   make = vehicle.vehicle.make
                                   model = vehicle.vehicle.model
                                   status = vehicle.status.ToString() })
                        ]
                    Common.Grid.gridContainer ({| cards = cards |})
                | other ->
                    Html.h1 (sprintf "Error!: %A" other)

        ]
    )

let render () =
    React.fragment [
        jumbotron()
        vehicles()
    ]
