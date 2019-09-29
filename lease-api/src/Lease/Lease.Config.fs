namespace Lease

open FsConfig
open System

type EventStoreConfig = 
    { [<DefaultValue("tcp")>]
      Protocol: string
      [<DefaultValue("localhost")>]
      Host: string 
      [<DefaultValue("1113")>]
      TcpPort: int
      [<DefaultValue("2113")>]
      HttpPort: int
      [<DefaultValue("admin")>]
      User: string 
      [<DefaultValue("changeit")>]
      Password: string } with

    member this.DiscoveryUri =
        let discoveryPort = if this.Protocol = "discover" then this.HttpPort else this.TcpPort
        sprintf "%s://%s:%d" this.Protocol this.Host discoveryPort |> Uri

type SeqConfig =
    { [<DefaultValue("http")>]
      Protocol: string
      [<DefaultValue("localhost")>]
      Host: string
      [<DefaultValue("5341")>]
      Port: int } with

    member this.Url =
        sprintf "%s://%s:%d" this.Protocol this.Host this.Port

type Config =
    { [<DefaultValue("true")>]
      Debug: bool
      [<DefaultValue("50051")>]
      Port: int
      [<CustomName("EVENTSTORE")>]
      EventStore: EventStoreConfig 
      Seq: SeqConfig } with
    static member Load() =
        match EnvConfig.Get<Config>() with
        | Ok config -> config
        | Error error -> failwithf "error loading config: %A" error
