namespace Reader

open Shared

type Config =
    { AppName: string
      Debug: bool
      ServerConfig: ServerConfig
      SeqConfig: SeqConfig
      MongoConfig: MongoConfig } with
    static member Load() =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = "Vehicle Reactor"
          Debug = (Env.getEnv "DEBUG" "true").ToLower() = "true"
          ServerConfig = ServerConfig.Load()
          SeqConfig = SeqConfig.Load()
          MongoConfig = MongoConfig.Load(secretsDir) }
