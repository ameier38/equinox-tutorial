module App

open Auth0
open Elmish
open Feliz
open Feliz.Router
open Feliz.MaterialUI
open FSharp.UMX
open GraphQL

type Url =
    | Root
    | Vehicles
    | Vehicle of VehicleId
    | Loading
    | NotFound
module Url =
    let parse (segments: string list) =
        Log.debug ("parse", segments)
        match segments with
        | ["vehicles"; vehicleId ] -> Vehicle (vehicleId |> UMX.tag<vehicleId>)
        | ["vehicles"] -> Vehicles
        | [] -> Root
        | (Route.Query ["code", _; "state", _])::_ -> Loading
        | _ -> NotFound
    let (|Parsed|) (segments: string list) = parse segments

type State =
    { CurrentUrl: Url }

type Msg =
    | UrlChanged of string list
    | NavigateToLanding
    | NavigateToVehicles

let init () =
    let currentUrl = Router.currentPath() |> Url.parse
    let initialState = { CurrentUrl  = currentUrl }
    Log.debug ("init", initialState)
    initialState, Cmd.none

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    Log.debug ("update", msg)
    match msg with
    | UrlChanged (Url.Parsed url) ->
        { state with CurrentUrl = url }, Cmd.none
    | NavigateToLanding ->
        state, Cmd.navigatePath "/"
    | NavigateToVehicles ->
        state, Cmd.navigatePath "/vehicles"

let renderPage =
    React.functionComponent(fun (props:{| currentUrl: Url; dispatch: Msg -> unit |}) ->
        let auth0 = React.useAuth0()
        match props.currentUrl, auth0.user with
        | Loading, _ ->
            Page.Loading.render()
        | _, Unresolved ->
            Page.Loading.render()
        | Root, Resolved (Authenticated ({ Role = Admin })) ->
            props.dispatch NavigateToVehicles
            Page.Loading.render()
        | Root, Resolved (Authenticated ({ Role = Customer }))
        | Root, Resolved (Anonymous) ->
            Page.Landing.render()
        | Vehicles, Resolved (Authenticated ({ Role = Admin })) ->
            Page.Vehicles.render()
        | Vehicles, Resolved (Authenticated ({ Role = Customer }))
        | Vehicles, Resolved (Anonymous) ->
            props.dispatch NavigateToLanding
            Page.Loading.render()
        | Vehicle vehicleId, _ ->
            Page.Vehicle.render { vehicleId = vehicleId }
        | NotFound, _ ->
            Page.NotFound.render {| navigateToLanding = (fun () -> props.dispatch NavigateToLanding) |}
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
                Auth0.domain Config.oauthConfig.Domain
                Auth0.clientId Config.oauthConfig.ClientId
                Auth0.redirectUri Config.appConfig.Url
                Auth0.audience Config.oauthConfig.Audience
                Auth0.defaultOnRedirectCallback (fun () -> dispatch NavigateToLanding)
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
                                    renderPage ({| currentUrl = state.CurrentUrl; dispatch = dispatch |})
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
