module Common.Grid

open Feliz
open Feliz.MaterialUI
open Feliz.Router

let useStyles = Styles.makeStyles(fun styles theme ->
    {|
        gridItem = styles.create [
            style.padding 10
        ]
        clickable = styles.create [
            style.cursor.pointer
        ]
    |}
)

let gridItem =
    React.functionComponent(fun (props:{| i: int; card: ReactElement |}) ->
        let c = useStyles()
        Mui.grid [
            prop.className c.gridItem 
            prop.key props.i
            grid.item true
            grid.xs._12
            grid.md._4
            grid.children [props.card]
        ]
    )

type VehicleCardProps =
    { vehicleId: string
      make: string
      model: string
      status: string }

let vehicleCard =
    React.functionComponent<VehicleCardProps>(fun (props) ->
        let c = useStyles()
        Mui.card [
            prop.className c.clickable
            prop.onClick (fun evt ->
                evt.preventDefault()
                Router.navigatePath("vehicles", props.vehicleId)
            )
            card.children [
                Mui.cardContent [
                    Mui.typography [
                        typography.variant.h6
                        prop.text (sprintf "%s %s" props.make props.model)
                    ]
                    Mui.typography [
                        typography.variant.body2
                        prop.text (sprintf "This vehicle is %A" props.status)
                    ]
                ]
            ]
        ]
    )

let skeletonCard =
    React.functionComponent<unit>(fun _ ->
        let c = useStyles()
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
    )

let gridContainer =
    React.functionComponent(fun (props:{| cards: ReactElement list |}) ->
        Mui.container [
            container.disableGutters true
            container.maxWidth.md
            container.children [
                Mui.grid [
                    grid.container true
                    grid.children [
                        for (i, card) in List.indexed props.cards ->
                            gridItem ({| i = i; card = card |})
                    ]
                ]
            ]
        ]
    )
