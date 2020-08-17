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
    { CurrentUrl: Url }

type Msg =
    | UrlChanged of string list

let init () =
    let currentUrl = Router.currentUrl() |> Url.parse
    let initialState =
        { CurrentUrl  = currentUrl }
    initialState, Cmd.none

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | UrlChanged url ->
        let currentUrl = Url.parse url
        { state with CurrentUrl = currentUrl }, Cmd.none

let renderPage (state:State) (dispatch:Msg -> unit) =
    match state.CurrentUrl with
    | LandingUrl ->
        Landing.render ()

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
                        GraphQL.publicUrl Config.graphqlConfig.PublicUrl
                        GraphQL.privateUrl Config.graphqlConfig.PrivateUrl
                        GraphQL.children [
                            Navigation.render { navigateToHome = id<unit> }
                            renderPage state dispatch
                        ]
                    ]
                ]
            ]
        ]
    ]
