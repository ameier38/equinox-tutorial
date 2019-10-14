namespace Lease

open System

type EventStoreConfig = 
    { User: string
      Password: string
      Host: string
      HttpPort: int
      DiscoveryUri: Uri } with
    static member Load() =
        let protocol = Some "tcp" |> Env.getEnv "EVENTSTORE_PROTOCOL"
        let httpPort = Some "2113" |> Env.getEnv "EVENTSTORE_HTTP_PORT" |> int
        let tcpPort = Some "1113" |> Env.getEnv "EVENTSTORE_TCP_PORT" |> int
        let host = Some "localhost" |> Env.getEnv "EVENTSTORE_HOST"
        let discoveryPort = if protocol = "discover" then httpPort else tcpPort
        { User = Some "admin" |> Env.getEnv "EVENTSTORE_USER"
          Password = Some "changeit" |> Env.getEnv "EVENTSTORE_PASSWORD"
          Host = host
          HttpPort = httpPort
          DiscoveryUri = sprintf "%s://%s:%d" protocol host discoveryPort |> Uri }

type SeqConfig =
    { Url: string } with
    static member Load() =
        let protocol = Some "http" |> Env.getEnv "SEQ_PROTOCOL"
        let host = Some "localhost" |> Env.getEnv "SEQ_HOST"
        let port = Some "5341" |> Env.getEnv "SEQ_PORT"
        { Url = sprintf "%s://%s:%s" protocol host port }

type ServerConfig =
    { Host: string
      Port: int } with
    static member Load() =
        { Host = Some "0.0.0.0" |> Env.getEnv "SERVER_HOST"
          Port = Some "50051" |> Env.getEnv "SERVER_PORT" |> int }

type Config =
    { Server: ServerConfig
      EventStore: EventStoreConfig 
      Seq: SeqConfig } with
    static member Load() =
        { Server = ServerConfig.Load()
          EventStore = EventStoreConfig.Load()
          Seq = SeqConfig.Load() }
