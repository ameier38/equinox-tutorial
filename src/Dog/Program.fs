open Suave
open Suave.Operators
open Suave.Successful
open Suave.Filters

[<EntryPoint>]
let main argv =
    let api = 
        choose
            [ GET >=> choose
                [ path "/" >=> OK "Welcome!" ] ]
    let config =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ]}
    startWebServer config api
    0 // return an integer exit code
