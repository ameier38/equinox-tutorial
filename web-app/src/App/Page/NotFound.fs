module Page.NotFound

open Elmish
open Feliz
open Feliz.MaterialUI

let useStyles = Styles.makeStyles(fun styles theme ->
    {|
        container = styles.create [
            style.display.flex
            style.flexWrap.wrap
            style.justifyContent.center
        ]
        text = styles.create [
            style.textAlign.center
            style.flexBasis (length.percent 100)
            style.paddingBottom 10
        ]
        button = styles.create [
            style.width 200
        ]
    |}
)

let render =
    React.functionComponent(fun (props: {| navigateToLanding: unit -> unit |}) ->
        let c = useStyles()
        Common.PageContainer.render true [
            Html.div [
                prop.className c.container
                prop.children [
                    Mui.typography [
                        prop.className c.text
                        typography.variant.h2
                        prop.text "Not Found"
                    ]
                    Mui.button [
                        prop.className c.button
                        button.variant.contained
                        prop.onClick (fun e ->
                            e.preventDefault()
                            props.navigateToLanding()
                        )
                        prop.text "Back to home"
                    ]
                ]
            ]
        ]
    )
