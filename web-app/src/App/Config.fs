module Config

let getPortComponent (port:string) =
    if port = "80" || port = "" then ""
    else sprintf ":%s" port

type GraphQLConfig =
    { PublicUrl: string
      PrivateUrl: string } with
    static member Load() =
        let scheme = Env.getEnv "GRAPHQL_SCHEME" "http"
        let host = Env.getEnv "GRAPHQL_HOST" "localhost"
        let port = Env.getEnv "GRAPHQL_PORT" "4000"
        let portComponent = getPortComponent port
        let publicUrl = sprintf "%s://%s%s/public" scheme host portComponent
        let privateUrl = sprintf "%s://%s%s" scheme host portComponent
        { PublicUrl = publicUrl
          PrivateUrl = privateUrl }

type OAuthConfig =
    { Domain: string
      ClientId: string
      Audience: string } with
    static member Load() =
        let domain = Env.getEnv "OAUTH_DOMAIN" "cosmicdealership.us.auth0.com"
        let clientId = Env.getEnv "OAUTH_CLIENT_ID" "test"
        let audience = Env.getEnv "OAUTH_AUDIENCE" "https://cosmicdealership.com"
        { Domain = domain
          ClientId = clientId
          Audience = audience }

type AppConfig =
    { Url: string } with
    static member Load() =
        let scheme = Env.getEnv "APP_SCHEME" "http"
        let host = Env.getEnv "APP_HOST" "localhost"
        let port = Env.getEnv "APP_PORT" "3000"
        let portComponent = getPortComponent port
        let url = sprintf "%s://%s%s" scheme host portComponent
        { Url = url }

let graphqlConfig = GraphQLConfig.Load()

let oauthConfig = OAuthConfig.Load()

let appConfig = AppConfig.Load()
