module Config

let getPortComponent (port:string) =
    if port = "80" || port = "" then ""
    else sprintf ":%s" port

type GraphqlClientConfig =
    { Url: string } with
    static member Load() =
        let scheme = Env.getEnv "FABLE_APP_GRAPHQL_API_SCHEME"
        let host = Env.getEnv "FABLE_APP_GRAPHQL_API_HOST"
        let port = Env.getEnv "FABLE_APP_GRAPHQL_API_PORT"
        let portComponent = getPortComponent port
        let url = sprintf "%s://%s%s" scheme host portComponent
        { Url = url }

type Auth0Config =
    { Domain: string
      ClientId: string } with
    static member Load() =
        let domain = Env.getEnv "FABLE_APP_AUTH0_DOMAIN"
        let clientId = Env.getEnv "FABLE_APP_AUTH0_CLIENT_ID"
        { Domain = domain
          ClientId = clientId }

type AppConfig =
    { Url: string } with
    static member Load() =
        let scheme = Env.getEnv "FABLE_APP_SCHEME"
        let host = Env.getEnv "FABLE_APP_HOST"
        let port = Env.getEnv "FABLE_APP_PORT"
        let portComponent = getPortComponent port
        let url = sprintf "%s://%s%s" scheme host portComponent
        { Url = url }

let graphqlClientConfig = GraphqlClientConfig.Load()

let auth0Config = Auth0Config.Load()

let appConfig = AppConfig.Load()
