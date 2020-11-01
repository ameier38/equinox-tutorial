module TestAdminApp

open canopy.classic
open Expecto
open Expecto.Flip

let startApp () =
    url "http://localhost:3000"
    waitForElement "#app"

[<Tests>]
let testCustomerApp =
    test "admin app" {
        1 |> Expect.equal "should equal 1" 1
    }
