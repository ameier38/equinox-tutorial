module Config

open System

type ChromeConfig =
    { DriverDir: string
      DriverUrl: string }
    static member Load() =
        let scheme = Env.getEnv "CHROME_SCHEME" "http"
        let host = Env.getEnv "CHROME_HOST" "localhost"
        let port = Env.getEnv "CHROME_PORT" "3000"
        let portComponent = Url.getPortComponent port
        { DriverUrl = sprintf "%s://%s%s/webdriver" scheme host portComponent
          DriverDir = Env.getEnv "CHROME_DRIVER_DIR" AppContext.BaseDirectory }

type ClientConfig =
    { Url: string
      Email: string
      Password: string }
    static member Load() =
        let scheme = Env.getEnv "CLIENT_SCHEME" "https"
        let host = Env.getEnv "CLIENT_HOST" "client.proxy"
        let port = Env.getEnv "CLIENT_PORT" "443"
        let portComponent = Url.getPortComponent port
        { Url = sprintf "%s://%s%s" scheme host portComponent
          Email = "customer@cosmicdealership.com"
          Password = "changeit" }
