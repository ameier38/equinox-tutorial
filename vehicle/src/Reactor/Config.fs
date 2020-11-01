namespace Reactor

open Shared

type Config =
    { AppName: string
      MongoConfig: MongoConfig
      EventStoreConfig: EventStoreConfig } with
    static member Load(appName:string) =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = appName
          MongoConfig = MongoConfig.Load(secretsDir)
          EventStoreConfig = EventStoreConfig.Load(secretsDir) }
