module App

open Auth0
open Elmish
open Feliz
open Feliz.Router
open Feliz.MaterialUI
open Feliz.UseElmish
open FSharp.UMX
open GraphQL

module Url =
    let (|Root|Vehicle|Vehicles|NotFound|) (segments: string list) =
        match segments with
        | ["vehicles"; vehicleId ] -> Vehicle (vehicleId |> UMX.tag<vehicleId>)
        | ["vehicles"] -> Vehicles
        | [] -> Root
        | _ -> NotFound

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
    printfn "%A" Config.appConfig
    match msg with
    | UrlChanged url ->
        match url with
        | Url.NotFound -> state, Cmd.ofMsg(UrlChanged [])
        | _ -> { state with CurrentUrl = url }, Cmd.none

let renderPage =
    React.functionComponent(fun _ ->
        let auth0 = React.useAuth0()
        let state, dispatch = React.useElmish(init, update, [||])
        match state.CurrentUrl with
        | Url.Root
        | Url.Vehicles ->
            match auth0.user with
            | Authenticated ({ Role = Admin }) ->
                Page.Vehicles.render ()
            | _ ->
                Page.Landing.render ()
        | Url.Vehicle vehicleId ->
            Page.Vehicle.render { vehicleId = vehicleId }
        | Url.NotFound -> Html.div "Page not found"
    )

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
                Auth0.domain Config.authConfig.Domain
                Auth0.clientId Config.authConfig.ClientId
                Auth0.redirectUri Config.appConfig.Url
                Auth0.audience Config.authConfig.Audience
                Auth0.children [
                    GraphQL.provider [
                        GraphQL.publicUrl Config.graphqlConfig.PublicUrl
                        GraphQL.privateUrl Config.graphqlConfig.PrivateUrl
                        GraphQL.children [
                            Mui.themeProvider [
                                themeProvider.theme theme
                                themeProvider.children [
                                    Navigation.render
                                        { transparent = match state.CurrentUrl with Url.Root -> true | _ -> false }
                                    renderPage()
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
