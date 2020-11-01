namespace Server

open Shared

type Config =
    { AppName: string
      ServerConfig: ServerConfig
      EventStoreConfig: EventStoreConfig } with
    static member Load(appName:string) =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = appName
          ServerConfig = ServerConfig.Load()
          EventStoreConfig = EventStoreConfig.Load(secretsDir) }
