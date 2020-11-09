open canopy.classic
open canopy.runner.classic
open canopy.types

canopy.configuration.chromeDir <- System.AppContext.BaseDirectory
canopy.configuration.webdriverPort <- Some 4444

let testEmail = "test@cosmicdealership.com"
let testPassword = @"?&x2Z@^%gLn}-A#nK4EBLu@j"

let startBrowser browserStartMode =
    start browserStartMode
    resize (1280, 960)

let startApp () =
    url "http://localhost:3000"
    waitForElement "#app"

"test login" &&& fun _ ->
    startApp()
    sleep 5
    click "Login"
    sleep 5
    on "https://cosmicdealership.us.auth0.com"
    waitForElement "input[type='email']"
    "input[type='email']" << testEmail
    "input[type='password']" << testPassword
    click "button[name='submit']"
    sleep 5
    on "https://cosmicdealership.com"
    displayed "Logout"

[<EntryPoint>]
let main argv =
    let browserStartMode =
        match argv with
        | [|"--headless"|] -> ChromeHeadless
        | _ -> Chrome
    try
        startBrowser browserStartMode
        run()
        quit()
        0
    with ex ->
        printfn "Error! %s" ex.Message
        quit()
        1
