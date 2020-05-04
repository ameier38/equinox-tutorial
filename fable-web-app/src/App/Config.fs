module Config

type GraphqlClientConfig =
    { Url: string } with
    static member Load() =
        let scheme = Env.getEnv "FABLE_APP_GRAPHQL_API_SCHEME"
        let host = Env.getEnv "FABLE_APP_GRAPHQL_API_HOST"
        let port = Env.getEnv "FABLE_APP_GRAPHQL_API_PORT"
        let port =
            if port = "80" || port = "" then ""
            else sprintf ":%s" port
        let url = sprintf "%s://%s%s" scheme host port
        { Url = url }

let graphqlClientConfig = GraphqlClientConfig.Load()
