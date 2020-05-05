module Navigation

open Elmish
open Feliz
open Feliz.MaterialUI

type State =
    { LoggedIn: bool }

type Msg =
    | NavigateToHome
    | NavigateToLogin

let init(): State * Cmd<Msg> =
    { LoggedIn = false }, Cmd.none

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | _ -> state, Cmd.none

let useStyles = Styles.makeStyles(fun styles theme ->
    {|
        navHomeButton = styles.create [
            style.color theme.palette.primary.contrastText
            style.fontFamily theme.typography.h6.fontFamily
            style.fontSize 16
            style.fontWeight 500
            style.textTransform.none
        ]

    |}
)

type NavigationProps =
    { state: State
      dispatch: Msg -> unit }

let render =
    React.functionComponent<NavigationProps>(fun props ->
        let c = useStyles()
        let theme = Styles.useTheme()
        let isGteMd = Hooks.useMediaQuery(theme.breakpoints.upMd)
        Mui.appBar [
            appBar.elevation 0
            appBar.square true
            appBar.position.fixed'
            appBar.color.transparent
            appBar.children [
                Mui.toolbar [
                    toolbar.disableGutters true
                    toolbar.children [
                        Mui.container [
                            container.maxWidth.md
                            container.disableGutters (not isGteMd)
                            container.children [
                                Mui.button [
                                    prop.className c.navHomeButton
                                    prop.onClick (fun e -> 
                                        e.preventDefault()
                                        props.dispatch NavigateToHome
                                    )
                                    button.variant.text
                                    button.children [ "Cosmic Dealership" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    )
