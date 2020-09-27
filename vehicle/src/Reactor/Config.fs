namespace Reactor

open Shared

type Config =
    { AppName: string
      Debug: bool
      SeqConfig: SeqConfig
      MongoConfig: MongoConfig
      EventStoreConfig: EventStoreConfig } with
    static member Load() =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = "Vehicle Reactor"
          Debug = (Env.getEnv "DEBUG" "true").ToLower() = "true"
          SeqConfig = SeqConfig.Load()
          MongoConfig = MongoConfig.Load(secretsDir)
          EventStoreConfig = EventStoreConfig.Load(secretsDir) }
