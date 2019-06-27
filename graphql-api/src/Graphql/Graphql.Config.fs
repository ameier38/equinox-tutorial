namespace Graphql

open FsConfig

[<Convention("LEASE_API")>]
type LeaseApiConfig =
    {
        [<DefaultValue("localhost")>]
        Host: string
        [<DefaultValue("50051")>]
        Port: int
    } with
    member this.ChannelTarget =
        sprintf "%s:%d" this.Host this.Port

type Config =
    {
        [<DefaultValue("true")>]
        Debug: bool
        [<DefaultValue("4000")>]
        Port: int
        LeaseApi: LeaseApiConfig
    }
module Config =
    let load () =
        match EnvConfig.Get<Config>() with
        | Ok config -> config
        | Error error -> failwithf "error loading config: %A" error
