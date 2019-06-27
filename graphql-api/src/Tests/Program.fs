// Learn more about F# at http://fsharp.org

open Expecto
open Expecto.Flip

let testDefault =
    test "default" {
        1 + 1
        |> Expect.equal "1 + 1 = 2" 2
    }

[<EntryPoint>]
let main argv =
    runTestsWithArgs defaultConfig argv testDefault
