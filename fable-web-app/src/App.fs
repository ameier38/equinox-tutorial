module App

open Elmish
open Feliz
open Feliz.Router

type State =
    { CurrentUrl: string list }

type Msg =
    | UrlChanged of string list

let init () =
    let currentUrl = Router.currentUrl()
    { CurrentUrl  = currentUrl }, Cmd.none

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | UrlChanged url -> { state with CurrentUrl = url }, Cmd.none

let render (state:State) (dispatch:Msg -> unit) =
    Router.router [
        Router.onUrlChanged (UrlChanged >> dispatch)
        Router.application [
            Html.h1 (sprintf "The current url is: %A" state.CurrentUrl)
        ]
    ]
