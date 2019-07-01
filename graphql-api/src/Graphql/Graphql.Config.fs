namespace Graphql

open FsConfig

type LeaseApiConfig =
    {
        [<DefaultValue("localhost")>]
        Host: string
        [<DefaultValue("50051")>]
        Port: int
    } with
    member this.ChannelTarget =
        sprintf "%s:%d" this.Host this.Port

type SeqConfig =
    {
        [<DefaultValue("http")>]
        Protocol: string
        [<DefaultValue("localhost")>]
        Host: string
        [<DefaultValue("5341")>]
        Port: int
    } with

    member this.Url =
        sprintf "%s://%s:%d" this.Protocol this.Host this.Port

type Config =
    {
        [<DefaultValue("true")>]
        Debug: bool
        [<DefaultValue("4000")>]
        Port: int
        LeaseApi: LeaseApiConfig
        Seq: SeqConfig
    }
module Config =
    let load () =
        match EnvConfig.Get<Config>() with
        | Ok config -> config
        | Error error -> failwithf "error loading config: %A" error
