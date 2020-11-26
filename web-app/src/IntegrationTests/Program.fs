open canopy.classic
open canopy.runner.classic
open canopy.types
open OpenQA.Selenium.Chrome

let chromeConfig = Config.ChromeConfig.Load()
let clientConfig = Config.ClientConfig.Load() 

canopy.configuration.chromeDir <- chromeConfig.DriverDir

let startBrowser () =
    let chromeOptions = ChromeOptions()
    chromeOptions.AddArgument("--no-sandbox")
    chromeOptions.AddArgument("--headless")
    let remote = Remote(chromeConfig.DriverUrl, chromeOptions.ToCapabilities())
    start remote

let startApp () =
    url clientConfig.Url
    waitForElement "#app"

"test login" &&& fun _ ->
    describe "starting app..."
    startApp()
    describe "clicking login..."
    click "Login"
    sleep 5
    describe "waiting for auth0 login..."
    on "https://cosmicdealership.us.auth0.com"
    waitForElement "input[type='email']"
    "input[type='email']" << clientConfig.Email
    "input[type='password']" << clientConfig.Password
    describe "submitting credentials..."
    click "button[name='submit']"
    sleep 5
    describe "waiting for cosmicdealership..."
    on "https://cosmicdealership.com"
    displayed "Logout"

[<EntryPoint>]
let main argv =
    try
        startBrowser()
        run()
        quit()
        0
    with ex ->
        printfn "Error! %s" ex.Message
        quit()
        1
