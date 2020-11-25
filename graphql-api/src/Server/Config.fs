namespace GraphqlApi

open Shared
open System.IO

[<RequireQualifiedAccess>]
type AppEnv =
    | Dev
    | Prod

type VehicleProcessorConfig =
    { Url: string } with
    static member Load() =
        let host = Env.getEnv "VEHICLE_PROCESSOR_HOST" "localhost"
        let port = Env.getEnv "VEHICLE_PROCESSOR_PORT" "50051" |> int
        { Url = sprintf "%s:%i" host port }

type VehicleReaderConfig =
    { Url: string } with
    static member Load() =
        let host = Env.getEnv "VEHICLE_READER_HOST" "localhost"
        let port = Env.getEnv "VEHICLE_READER_PORT" "50052" |> int
        { Url = sprintf "%s:%i" host port }

type SeqConfig =
    { Url: string } with
    static member Load() =
        let host = Env.getEnv "SEQ_HOST" "localhost" 
        let port = Env.getEnv "SEQ_PORT" "5341" |> int
        { Url = sprintf "http://%s:%d" host port }

type OAuthConfig =
    { Audience: string
      Issuer: string
      // NB: only used for development
      CertPath: string } with
    static member Load(secretsDir:string) =
        let secretName = Env.getEnv "OAUTH_SECRET" "oauth"
        let audience = Env.getEnv "OAUTH_AUDIENCE" "https://cosmicdealership.com"
        let issuer = Env.getEnv "OAUTH_ISSUER" "https://cosmicdealership.us.auth0.com/"
        let certPath = Path.Join(secretsDir, secretName, "oauth.crt")
        { Audience = audience
          Issuer = issuer
          CertPath = certPath }

type ServerConfig =
    { Host: string
      Port: int } with
    static member Load() =
        let host = Env.getEnv "SERVER_HOST" "0.0.0.0" 
        let port = Env.getEnv "SERVER_PORT" "4000" |> int
        { Host = host
          Port = port }

type Config =
    { Debug: bool
      AppEnv: AppEnv
      ServerConfig: ServerConfig
      OAuthConfig: OAuthConfig
      VehicleProcessorConfig: VehicleProcessorConfig
      VehicleReaderConfig: VehicleReaderConfig
      SeqConfig: SeqConfig } with
    static member Load() =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        let appEnv =
            match Env.getEnv "APP_ENV" "dev" with
            | "dev" -> AppEnv.Dev
            | "prod" -> AppEnv.Prod
            | other -> failwithf "%s is not a valid app env" other
        { Debug = Env.getEnv "DEBUG" "false" |> bool.Parse
          AppEnv = appEnv
          ServerConfig = ServerConfig.Load()
          OAuthConfig = OAuthConfig.Load(secretsDir)
          VehicleProcessorConfig = VehicleProcessorConfig.Load()
          VehicleReaderConfig = VehicleReaderConfig.Load()
          SeqConfig = SeqConfig.Load() }
