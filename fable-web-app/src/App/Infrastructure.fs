[<AutoOpen>]
module Infrastructure

open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open FSharp.UMX
open System

type [<Measure>] pageToken
type PageToken = string<pageToken>
module PageToken =
    let fromString (s:string) = UMX.tag<pageToken> s
    let toString (pageToken:PageToken) = UMX.untag pageToken

type [<Measure>] pageSize
type PageSize = int<pageToken>
module PageSize =
    let fromInt (i:int) = UMX.tag<pageSize> i
    let toInt (pageSize:PageSize) = UMX.untag pageSize

type [<Measure>] vehicleId
type VehicleId = string<vehicleId>
module VehicleId =
    let fromString (s:string) = UMX.tag<vehicleId> s
    let toString (vehicleId:VehicleId) = UMX.untag vehicleId

type AsyncOperation<'P,'T> =
    | Started of 'P
    | Finished of 'T

type Deferred<'T> =
    | NotStarted
    | InProgress
    | Resolved of 'T

module Cow =
    let says (msg:string) = String.Format(@"
 --------------------------
< {0} >
 --------------------------
        \   ^__^
         \  (xx)\_______
            (__)\       )\/\
             U  ||----w |
                ||     ||
", msg)

[<RequireQualifiedAccess>]
module Env =
    [<Emit("process.env[$0] ? process.env[$0] : ''")>]
    let getEnv (key:string): string = jsNative

[<RequireQualifiedAccess>]
module Log =
    let info (msg:obj) =
        Fable.Core.JS.console.info(msg)

    let debug (msg:obj) =
#if DEVELOPMENT
        Fable.Core.JS.console.info(msg)
#else
        ()
#endif

    let error (error:obj) =
#if DEVELOPMENT
        Fable.Core.JS.console.error(error)
#else
        ()
#endif

[<RequireQualifiedAccess>]
module Cmd =
    let fromAsync (work:Async<'M>): Cmd<'M> =
        let asyncCmd (dispatch:'M -> unit): unit =
            let asyncDispatch =
                async {
                    let! msg = work
                    dispatch msg
                }
            Async.StartImmediate asyncDispatch
        Cmd.ofSub asyncCmd

[<RequireQualifiedAccess>]
module Error =
    let renderError () =
        Html.div [
            prop.style [
                style.display.flex
                style.justifyContent.center
                style.overflowX.hidden
            ]
            prop.children [
                Html.pre (Cow.says "Oops!")
            ]
        ]

[<RequireQualifiedAccess>]
module Image =
    let inline load (relativePath:string): string = importDefault relativePath
