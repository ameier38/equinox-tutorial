namespace Server

open Shared

type Config =
    { AppName: string
      Debug: bool
      ServerConfig: ServerConfig
      EventStoreConfig: EventStoreConfig 
      SeqConfig: SeqConfig } with
    static member Load() =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = "Vehicle Server"
          Debug = Env.getEnv "DEBUG" "false" |> bool.Parse
          ServerConfig = ServerConfig.Load()
          EventStoreConfig = EventStoreConfig.Load(secretsDir)
          SeqConfig = SeqConfig.Load() }
