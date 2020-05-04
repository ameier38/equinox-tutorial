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
      LandingState: Landing.State }

type Msg =
    | UrlChanged of string list
    | LandingMsg of Landing.Msg

let init () =
    let currentUrl = Router.currentUrl() |> Url.parse
    let landingState, landingCmd = Landing.init()
    let cmd =
        [ landingCmd |> Cmd.map LandingMsg ]
        |> Cmd.batch
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

let renderApp =
    React.functionComponent<AppProps>(fun props ->
        let theme = Styles.useTheme()
        let isGteMd = Hooks.useMediaQuery(theme.breakpoints.upMd)
        Mui.container [
            container.disableGutters (not isGteMd)
            container.maxWidth.md
            container.children [
                renderPage props.state props.dispatch
            ]
        ]
    )

let render (state:State) (dispatch:Msg -> unit) =
    Router.router [
        Router.pathMode
        Router.onUrlChanged (fun url ->
            Fable.Core.JS.console.info(url)
            dispatch (UrlChanged url)
        )
        Router.application [ 
            Mui.cssBaseline []
            renderApp { state = state; dispatch = dispatch }
        ]
    ]
