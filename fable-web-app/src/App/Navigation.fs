module Navigation

open Elmish
open Feliz
open Feliz.MaterialUI

type State =
    { _empty: bool }

type Msg =
    | NavigateToHome
    | NavigateToLogin

let init(): State * Cmd<Msg> =
    { _empty = true }, Cmd.none

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | _ -> state, Cmd.none

let useStyles = Styles.makeStyles(fun styles theme ->
    {|
        navContainer = styles.create [
            style.display.flex
            style.justifyContent.spaceBetween
        ]
        navHomeButton = styles.create [
            style.color theme.palette.primary.contrastText
            style.fontFamily theme.typography.h6.fontFamily
            style.fontSize 16
            style.fontWeight 500
            style.textTransform.none
        ]
        loginButton = styles.create [
            style.color theme.palette.primary.contrastText
        ]
    |}
)

type NavigationProps =
    { state: State
      dispatch: Msg -> unit }

let navigation =
    React.functionComponent<NavigationProps>(fun props ->
        let c = useStyles()
        let auth0 = Auth0.useAuth0()
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
                            prop.className c.navContainer
                            container.maxWidth.md
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
                                if not auth0.isLoading then
                                    Html.div [
                                        Mui.button [
                                            prop.className c.loginButton
                                            prop.onClick (fun e ->
                                                e.preventDefault()
                                                if auth0.isAuthenticated then auth0.logout()
                                                else auth0.login()
                                            )
                                            button.children [
                                                if auth0.isAuthenticated then "Logout"
                                                else "Login"
                                            ]
                                        ]
                                        if not auth0.isAuthenticated then
                                            Mui.button [
                                                prop.onClick (fun e ->
                                                    e.preventDefault()
                                                    auth0.login()
                                                )
                                                button.variant.contained
                                                button.color.primary
                                                button.children ["Signup"]
                                            ]
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    )

let render state dispatch = navigation { state = state; dispatch = dispatch }
