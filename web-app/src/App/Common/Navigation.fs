module Navigation

open Auth0
open Feliz
open Feliz.MaterialUI
open Feliz.Router

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
           loginButton =
               styles.create [
                   style.color.white
               ] |})

type NavigationProps = { transparent: bool }

let homeButton =
    React.functionComponent<unit>(fun _ ->
        let c = useStyles()
        Mui.button [
            prop.className c.navHomeButton
            prop.onClick (fun e ->
                e.preventDefault ()
                Router.navigatePath (""))
            button.variant.text
            button.children [ "Cosmic Dealership" ]
        ]
    )

let loginButton =
    React.functionComponent(fun (props: {| isAuthenticated: bool; login: unit -> unit; logout: unit -> unit |}) ->
        let c = useStyles()
        Mui.button [
            prop.className c.loginButton
            prop.onClick (fun e ->
                e.preventDefault()
                if props.isAuthenticated then props.logout() else props.login())
            button.children [
                if props.isAuthenticated then "Logout" else "Login"
            ]
        ]
    )

let signupButton =
    React.functionComponent(fun (props: {| login: unit -> unit |}) ->
        Mui.button [
            prop.onClick (fun e ->
                e.preventDefault()
                props.login())
            button.variant.contained
            button.color.primary
            button.children [ "Signup" ]
        ]
    )

let render =
    React.functionComponent<NavigationProps> (fun props ->
        let c = useStyles()
        let auth0 = React.useAuth0()
        Mui.appBar [
            appBar.elevation 0
            appBar.square true
            appBar.position.absolute
            if props.transparent then appBar.color.transparent else appBar.color.secondary
            appBar.children [
                Mui.toolbar [
                    toolbar.disableGutters true
                    toolbar.children [
                        Mui.container [
                            prop.className c.navContainer
                            container.maxWidth.md
                            container.children [
                                homeButton()
                                match auth0.user with
                                | Unresolved -> ()
                                | Resolved user ->
                                    let isAuthenticated = match user with Authenticated _ -> true | Anonymous -> false
                                    Html.div [
                                        loginButton
                                            {| isAuthenticated = isAuthenticated;
                                               login = auth0.login;
                                               logout = fun () -> auth0.logout(Config.appConfig.Url) |}
                                        if not isAuthenticated then
                                            signupButton ({| login = auth0.login |})
                                    ]
                            ]
                        ]
                    ]
                ]
            ]
        ])
