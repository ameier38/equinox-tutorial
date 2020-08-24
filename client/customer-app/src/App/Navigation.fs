module Navigation

open Auth0
open Elmish
open Fable.Core.JsInterop
open Feliz
open Feliz.MaterialUI
open Feliz.Router

module Url =
    let (|Home|Other|) (segments: string list) =
        match segments with
        | [] -> Home
        | _ -> Other

module Icon =
    let dashboard: string =
        importDefault "@material-ui/icons/Dashboard"

let useStyles =
    Styles.makeStyles (fun styles theme ->
        {| navContainer =
               styles.create [
                   style.display.flex
                   style.justifyContent.spaceBetween
               ]
           navHomeButton =
               styles.create [
                   style.color theme.palette.primary.contrastText
                   style.fontFamily theme.typography.h6.fontFamily
                   style.fontSize 16
                   style.fontWeight 500
                   style.textTransform.none
               ]
           loginButton = styles.create [ style.color theme.palette.primary.contrastText ] |})

type NavigationProps =
    { currentUrl: string list }

let render =
    React.functionComponent<NavigationProps>(fun props ->
        let c = useStyles ()
        let auth0 = React.useAuth0 ()
        Mui.appBar [
            appBar.elevation 0
            appBar.square true
            appBar.position.absolute
            match props.currentUrl with
            | Url.Home -> appBar.color.transparent
            | Url.Other -> appBar.color.secondary
            appBar.children
                [ Mui.toolbar [
                    toolbar.disableGutters true
                    toolbar.children
                        [ Mui.container [
                            prop.className c.navContainer
                            container.maxWidth.md
                            container.children [
                                Mui.button [
                                    prop.className c.navHomeButton
                                    prop.onClick (fun e ->
                                        e.preventDefault ()
                                        Router.navigatePath("")
                                    )
                                    button.variant.text
                                    button.children [ "Cosmic Dealership" ]
                                ]
                                if not auth0.isLoading then
                                    Html.div [
                                        Mui.button [
                                            prop.className c.loginButton
                                            prop.onClick (fun e ->
                                                e.preventDefault ()
                                                if auth0.isAuthenticated then auth0.logout () else auth0.login ())
                                            button.children [ if auth0.isAuthenticated then "Logout" else "Login" ]
                                        ]
                                        if not auth0.isAuthenticated then
                                            Mui.button [
                                                prop.onClick (fun e ->
                                                    e.preventDefault ()
                                                    auth0.login ())
                                                button.variant.contained
                                                button.color.primary
                                                button.children [ "Signup" ]
                                            ]
                                    ]
                            ]
                          ] ]
                  ] ]
        ])
