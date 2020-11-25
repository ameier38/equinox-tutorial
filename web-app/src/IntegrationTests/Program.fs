open canopy.classic
open canopy.runner.classic
open canopy.types
open OpenQA.Selenium.Chrome
open System
open System.IO

let getEnv (key:string) (defaultValue:string) =
    match Environment.GetEnvironmentVariable(key) with
    | value when String.IsNullOrEmpty(value) -> defaultValue
    | value -> value

let getSecret (secretsDir:string) (secretName:string) (secretKey:string) (defaultValue:string) =
    let secretPath = Path.Combine(secretsDir, secretName, secretKey)
    if File.Exists(secretPath) then
        File.ReadAllText(secretPath).Trim()
    else
        defaultValue

let chromeDir = getEnv "CHROME_DIR" AppContext.BaseDirectory
let testEmail = "customer@cosmicdealership.com"
let testPassword = "changeit"
let appHost = getEnv "WEB_APP_HOST" "web-app.proxy"
let appUrl = sprintf "https://%s" appHost

canopy.configuration.chromeDir <- chromeDir
canopy.configuration.webdriverPort <- Some 4444

let startBrowser () =
    let chromeOptions = ChromeOptions()
    chromeOptions.AddArgument("--no-sandbox")
    chromeOptions.AddArgument("--headless")
    let remote = Remote("http://chrome:3000/webdriver", chromeOptions.ToCapabilities())
    start remote

let startApp () =
    url appUrl
    waitForElement "#app"

"test loaded" &&& fun _ ->
    startApp()
    if title() <> "Cosmic Dealership" then failwithf "wrong title"

"test login" &&& fun _ ->
    printfn "starting app..."
    startApp()
    sleep 5
    let html = js "return document.documentElement.outerHTML" |> string
    printfn "html: %s" html
    printfn "clicking login..."
    click "Login"
    sleep 5
    printfn "waiting for auth0 login..."
    on "https://cosmicdealership.us.auth0.com"
    waitForElement "input[type='email']"
    "input[type='email']" << testEmail
    "input[type='password']" << testPassword
    printfn "submitting credentials..."
    click "button[name='submit']"
    sleep 5
    printfn "waiting for cosmicdealership..."
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
