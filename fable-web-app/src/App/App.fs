module App

open Elmish
open Feliz
open Feliz.Router
open Feliz.MaterialUI

type Url =
    | LandingUrl
module Url =
    let parse (url:string list) =
        match url with
        | _ -> LandingUrl

type State =
    { CurrentUrl: Url
      NavigationState: Navigation.State
      LandingState: Landing.State }

type Msg =
    | UrlChanged of string list
    | NavigationMsg of Navigation.Msg
    | LandingMsg of Landing.Msg

let init () =
    let currentUrl = Router.currentUrl() |> Url.parse
    let navigationState, navigationCmd = Navigation.init()
    let landingState, landingCmd = Landing.init()
    let cmd =
        [ navigationCmd |> Cmd.map NavigationMsg
          landingCmd |> Cmd.map LandingMsg ]
        |> Cmd.batch
    let initialState =
        { CurrentUrl  = currentUrl
          NavigationState = navigationState
          LandingState = landingState }
    initialState, cmd

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | UrlChanged url ->
        let currentUrl = Url.parse url
        { state with CurrentUrl = currentUrl }, Cmd.none
    | NavigationMsg msg ->
        let navigationState, navigationCmd = Navigation.update msg state.NavigationState
        let newState =
            { state with
                NavigationState = navigationState }
        newState, navigationCmd |> Cmd.map NavigationMsg
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
        Router.onUrlChanged (fun url ->
            Fable.Core.JS.console.info(url)
            dispatch (UrlChanged url)
        )
        Router.application [ 
            Mui.cssBaseline []
            Navigation.render { state = state.NavigationState; dispatch = (NavigationMsg >> dispatch) }
            renderPage state dispatch
        ]
    ]
