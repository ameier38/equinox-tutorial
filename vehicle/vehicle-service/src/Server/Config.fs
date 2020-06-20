namespace Server

open Shared
open System

type EventStoreConfig = 
    { User: string
      Password: string
      DiscoveryUri: Uri } with
    static member Load(secretsDir:string) =
        let getSecret = Env.getSecret secretsDir "eventstore"
        let scheme = Env.getEnv "EVENTSTORE_SCHEME" "tcp" 
        let httpPort = Env.getEnv "EVENTSTORE_HTTP_PORT" "2113" |> int
        let tcpPort = Env.getEnv "EVENTSTORE_TCP_PORT" "1113" |> int
        let host = Env.getEnv "EVENTSTORE_HOST" "localhost"
        let discoveryPort = if scheme = "discover" then httpPort else tcpPort
        let user = getSecret "user" "EVENTSTORE_USER" "admin"
        let password = getSecret "password" "EVENTSTORE_PASSWORD" "changeit"
        { User = user
          Password = password
          DiscoveryUri = sprintf "%s://%s:%d" scheme host discoveryPort |> Uri }

type SeqConfig =
    { Url: string } with
    static member Load() =
        let scheme = Env.getEnv "SEQ_SCHEME" "http"
        let host = Env.getEnv "SEQ_HOST" "localhost"
        let port = Env.getEnv "SEQ_PORT" "5341"
        { Url = sprintf "%s://%s:%s" scheme host port }

type ServerConfig =
    { Host: string
      Port: int } with
    static member Load() =
        { Host = Env.getEnv "SERVER_HOST" "0.0.0.0"
          Port = Env.getEnv "SERVER_PORT" "50051" |> int }

type Config =
    { AppName: string
      Debug: bool
      Server: ServerConfig
      EventStore: EventStoreConfig 
      Seq: SeqConfig } with
    static member Load() =
        let secretsDir = Env.getEnv "SECRETS_DIR" "/var/secrets"
        { AppName = "Vehicle Server"
          Debug = Env.getEnv "DEBUG" "false" |> bool.Parse
          Server = ServerConfig.Load()
          EventStore = EventStoreConfig.Load(secretsDir)
          Seq = SeqConfig.Load() }
