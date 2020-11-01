namespace Reader

open Shared

type Config =
    { AppName: string
      ServerConfig: ServerConfig
      MongoConfig: MongoConfig } with
    static member Load(appName:string) =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = appName
          ServerConfig = ServerConfig.Load()
          MongoConfig = MongoConfig.Load(secretsDir) }
