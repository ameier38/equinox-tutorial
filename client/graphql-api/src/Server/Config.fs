namespace Server

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
      VehicleApiConfig: VehicleApiConfig
      MongoConfig: MongoConfig
      SeqConfig: SeqConfig } with
    static member Load() =
        let secretsDir = Shared.Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = "GraphQL Server"
          Debug = Shared.Env.getEnv "DEBUG" "true" |> bool.Parse
          ServerConfig = ServerConfig.Load()
          VehicleApiConfig = VehicleApiConfig.Load()
          MongoConfig = MongoConfig.Load(secretsDir)
          SeqConfig = SeqConfig.Load() }
