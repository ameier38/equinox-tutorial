module App

open Auth0
open Elmish
open Feliz
open Feliz.Router
open Feliz.MaterialUI
open GraphQL

type Url =
    | LandingUrl
module Url =
    let parse (url:string list) =
        match url with
        | _ -> LandingUrl

type State =
    { CurrentUrl: Url
      LandingState: unit }

type Msg =
    | UrlChanged of string list
    | LandingMsg of Landing.Msg

let init () =
    let currentUrl = Router.currentUrl() |> Url.parse
    let landingState, landingCmd = Landing.init()
    let cmd = Cmd.batch [
        landingCmd |> Cmd.map LandingMsg
    ]
    let initialState =
        { CurrentUrl  = currentUrl
          LandingState = landingState }
    initialState, cmd

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | UrlChanged url ->
        let currentUrl = Url.parse url
        { state with CurrentUrl = currentUrl }, Cmd.none
    | LandingMsg msg ->
        let landingState, landingCmd = Landing.update msg state.LandingState
        let newState =
            { state with
                LandingState = landingState }
        newState, landingCmd |> Cmd.map LandingMsg

let renderPage (state:State) (dispatch:Msg -> unit) =
    match state.CurrentUrl with
    | LandingUrl ->
        Landing.render state.LandingState (LandingMsg >> dispatch)

type AppProps =
    { state: State
      dispatch: Msg -> unit }

let render (state:State) (dispatch:Msg -> unit) =
    Router.router [
        Router.pathMode
        Router.onUrlChanged (UrlChanged >> dispatch)
        Router.application [ 
            Mui.cssBaseline []
            Auth0.provider [
                Auth0.domain Config.auth0Config.Domain
                Auth0.clientId Config.auth0Config.ClientId
                Auth0.redirectUri Config.appConfig.Url
                Auth0.audience Config.auth0Config.Audience
                Auth0.children [
                    GraphQL.provider [
                        GraphQL.url Config.graphqlConfig.Url
                        GraphQL.children [
                            Common.navigation { navigateToHome = id<unit> }
                            renderPage state dispatch
                        ]
                    ]
                ]
            ]
        ]
    ]
