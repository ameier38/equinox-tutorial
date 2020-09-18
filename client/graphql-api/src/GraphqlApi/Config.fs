namespace GraphqlApi

type VehicleApiConfig =
    { Url: string } with
    static member Load() =
        let host = Shared.Env.getEnv "VEHICLE_API_HOST" "localhost"
        let port = Shared.Env.getEnv "VEHICLE_API_PORT" "50051" |> int
        { Url = sprintf "%s:%i" host port }

type MongoConfig =
    { Url: string
      Host: string
      Port: int
      User: string
      Password: string
      Database: string } with
    static member Load(secretsDir:string) =
        let getMongoSecret = Shared.Env.getSecret secretsDir "mongo"
        let host = getMongoSecret "host" "MONGO_HOST" "localhost"
        let port = getMongoSecret "port" "MONGO_PORT" "27017" |> int
        let user = getMongoSecret "user" "MONGO_USER" "admin"
        let password = getMongoSecret "password" "MONGO_PASSWORD" "changeit"
        let url = sprintf "mongodb://%s:%i" host port
        let database = getMongoSecret "database" "MONGO_DATABASE" "dealership"
        { Url = url
          Host = host
          Port = port
          User = user
          Password = password
          Database = database }

type SeqConfig =
    { Url: string } with
    static member Load() =
        let host = Shared.Env.getEnv "SEQ_HOST" "localhost" 
        let port = Shared.Env.getEnv "SEQ_PORT" "5341" |> int
        { Url = sprintf "http://%s:%d" host port }

type Auth0Config =
    { Audience: string
      Issuer: string
      Secret: string } with
    static member Load(secretsDir:string) =
        let getSecret = Shared.Env.getSecret secretsDir "auth0"
        let audience = getSecret "audience" "AUTH0_AUDIENCE" "https://cosmicdealership.com"
        let issuer = getSecret "issuer" "AUTH0_ISSUER" "https://ameier38.auth0.com/"
        // see README to generate test secret
        let secret = getSecret "secret" "AUTH0_SECRET" "671f54ce0c540f78ffe1e26dcf9c2a047aea4fda"
        { Audience = audience
          Issuer = issuer
          Secret = secret }

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
      VehicleApiConfig: VehicleApiConfig
      MongoConfig: MongoConfig
      SeqConfig: SeqConfig } with
    static member Load() =
        let secretsDir = Shared.Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = "GraphQL Server"
          Debug = Shared.Env.getEnv "DEBUG" "true" |> bool.Parse
          ServerConfig = ServerConfig.Load()
          Auth0Config = Auth0Config.Load(secretsDir)
          VehicleApiConfig = VehicleApiConfig.Load()
          MongoConfig = MongoConfig.Load(secretsDir)
          SeqConfig = SeqConfig.Load() }
