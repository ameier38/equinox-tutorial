module Config

open Shared

type GraphqlConfig =
    { Url: string }
    static member Load() =
        let scheme = Env.getEnv "GRAPHQL_SCHEME" "http"
        let host = Env.getEnv "GRAPHQL_HOST" "localhost" 
        let port = Env.getEnv "GRAPHQL_PORT" "4000" |> int
        { Url = sprintf "%s://%s:%i" scheme host port }

type OAuthConfig =
    { PrivateKey: Secret<string>
      Issuer: string
      Audience: string }
    static member Load() =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/dev/secrets/cosmicdealership"
        { PrivateKey = Secret.getSecret secretsDir "oauth" "private-key" "OAUTH_PRIVATE_KEY" ""
          Issuer = Env.getEnv "OAUTH_ISSUER" "https://cosmicdealership.us.auth0.com/"
          Audience = Env.getEnv "OAUTH_AUDIENCE" "https://cosmicdealership.com" }
