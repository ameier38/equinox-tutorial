namespace Lease

open FsConfig
open System

[<Convention("EVENTSTORE")>]
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


type Config =
    { Debug: bool
      Port: int16
      EventStore: EventStoreConfig }
module Config =
    let load () =
        match EnvConfig.Get<Config>() with
        | Ok config -> config
        | Error error -> failwithf "error loading config: %A" error
