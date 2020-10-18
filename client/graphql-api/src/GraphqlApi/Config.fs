namespace GraphqlApi

type VehicleProcessorConfig =
    { Url: string } with
    static member Load() =
        let host = Shared.Env.getEnv "VEHICLE_PROCESSOR_HOST" "localhost"
        let port = Shared.Env.getEnv "VEHICLE_PROCESSOR_PORT" "50051" |> int
        { Url = sprintf "%s:%i" host port }

type VehicleReaderConfig =
    { Url: string } with
    static member Load() =
        let host = Shared.Env.getEnv "VEHICLE_READER_HOST" "localhost"
        let port = Shared.Env.getEnv "VEHICLE_READER_PORT" "50052" |> int
        { Url = sprintf "%s:%i" host port }

type SeqConfig =
    { Url: string } with
    static member Load() =
        let host = Shared.Env.getEnv "SEQ_HOST" "localhost" 
        let port = Shared.Env.getEnv "SEQ_PORT" "5341" |> int
        { Url = sprintf "http://%s:%d" host port }

type Auth0Config =
    { Audience: string
      Issuer: string
      ClientSecret: string } with
    static member Load(secretsDir:string) =
        let secretName = Shared.Env.getEnv "AUTH0_SECRET" "auth0"
        let getSecret = Shared.Env.getSecret secretsDir secretName
        let audience = Shared.Env.getEnv "AUTH0_AUDIENCE" "https://cosmicdealership.com"
        let issuer = Shared.Env.getEnv "AUTH0_ISSUER" "https://ameier38.auth0.com/"
        // see README to generate test client secret
        let clientSecret = getSecret "client-secret" "AUTH0_CLIENT_SECRET" "671f54ce0c540f78ffe1e26dcf9c2a047aea4fda"
        { Audience = audience
          Issuer = issuer
          ClientSecret = clientSecret }

type ServerConfig =
    { Host: string
      Port: int } with
    static member Load() =
        let host = Shared.Env.getEnv "SERVER_HOST" "0.0.0.0" 
        let port = Shared.Env.getEnv "SERVER_PORT" "4000" |> int
        { Host = host
          Port = port }

type Config =
    { AppName: string
      Debug: bool
      ServerConfig: ServerConfig
      Auth0Config: Auth0Config
      VehicleProcessorConfig: VehicleProcessorConfig
      VehicleReaderConfig: VehicleReaderConfig
      SeqConfig: SeqConfig } with
    static member Load() =
        let secretsDir = Shared.Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = "GraphQL Server"
          Debug = Shared.Env.getEnv "DEBUG" "true" |> bool.Parse
          ServerConfig = ServerConfig.Load()
          Auth0Config = Auth0Config.Load(secretsDir)
          VehicleProcessorConfig = VehicleProcessorConfig.Load()
          VehicleReaderConfig = VehicleReaderConfig.Load()
          SeqConfig = SeqConfig.Load() }
