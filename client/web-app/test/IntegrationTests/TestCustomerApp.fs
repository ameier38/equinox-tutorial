module TestCustomerApp

open canopy.classic
open Expecto
open System.Threading

let startApp () =
    url "http://localhost:3000"
    waitForElement "#app"

let sleep () =
    Thread.Sleep(2000)

[<Tests>]
let testCustomerApp =
    test "customer app" {
        startApp()
        sleep()
        click "Falcon 0"
        sleep()
        waitForElement "#title"
        sleep()
        "#title" == "Falcon 0"
    }
