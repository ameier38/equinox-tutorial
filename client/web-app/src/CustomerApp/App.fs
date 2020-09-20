module App

open Auth0
open Elmish
open Feliz
open Feliz.Router
open Feliz.MaterialUI
open FSharp.UMX
open GraphQL

module Url =
    let (|Landing|Vehicle|) (segments: string list) =
        match segments with
        | ["vehicles"; vehicleId ] ->
            Vehicle (vehicleId |> UMX.tag<vehicleId>)
        | _ -> Landing

type State =
    { CurrentUrl: string list }

type Msg =
    | UrlChanged of string list

let init () =
    let currentUrl = Router.currentUrl()
    let initialState =
        { CurrentUrl  = currentUrl }
    initialState, Cmd.none

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | UrlChanged url ->
        { state with CurrentUrl = url }, Cmd.none

let renderPage (state:State) (dispatch:Msg -> unit) =
    match state.CurrentUrl with
    | Url.Landing ->
        PublicApp.Landing.render ()
    | Url.Vehicle vehicleId ->
        PublicApp.Vehicle.render { vehicleId = vehicleId }

let theme = Styles.createMuiTheme([
    theme.palette.primary Colors.blueGrey
    theme.palette.secondary Colors.grey
])

type AppProps =
    { state: State
      dispatch: Msg -> unit }

let render (state:State) (dispatch:Msg -> unit) =
    React.router [
        router.pathMode
        router.onUrlChanged (UrlChanged >> dispatch)
        router.children [
            Mui.cssBaseline []
            Auth0.provider [
                Auth0.domain Config.auth0Config.Domain
                Auth0.clientId Config.auth0Config.ClientId
                Auth0.redirectUri Config.appConfig.Url
                Auth0.audience Config.auth0Config.Audience
                Auth0.children [
                    GraphQL.provider [
                        GraphQL.publicUrl Config.graphqlConfig.PublicUrl
                        GraphQL.privateUrl Config.graphqlConfig.PrivateUrl
                        GraphQL.children [
                            Mui.themeProvider [
                                themeProvider.theme theme
                                themeProvider.children [
                                    Navigation.render
                                        { transparent = match state.CurrentUrl with Url.Landing -> true | _ -> false }
                                    renderPage state dispatch
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]