// Learn more about F# at http://fsharp.org

open canopy.classic
open canopy.types
open Expecto

let startBrowser () =
    start Chrome
    resize (1280, 960)

[<EntryPoint>]
let main argv =
    try
        try
            startBrowser()
            runTestsInAssembly { defaultConfig with runInParallel = false } argv
        with ex ->
            printfn "Error! %s" ex.Message
            -1
    finally
        quit()
