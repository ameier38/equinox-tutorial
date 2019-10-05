namespace Graphql

open System

module Env = 
    let getEnv (key:string) (defaultValueOpt:string option) =
        match Environment.GetEnvironmentVariable(key), defaultValueOpt with
        | value, Some defaultValue when String.IsNullOrEmpty(value) -> defaultValue
        | value, None when String.IsNullOrEmpty(value) -> failwithf "envVar %s is not defined" key
        | value, _ -> value

type LeaseApiConfig =
    { Host: string
      Port: int
      ChannelTarget: string } with
    static member Load() =
        let host = Some "localhost" |> Env.getEnv "LEASE_API_HOST"
        let port = Some "50051" |> Env.getEnv "LEASE_API_PORT" |> int
        { Host = host
          Port = port
          ChannelTarget = sprintf "%s:%d" host port }

type SeqConfig =
    { Host: string
      Port: int
      Url: string } with
    static member Load() =
        let host = Some "localhost" |> Env.getEnv "SEQ_HOST"
        let port = Some "5341" |> Env.getEnv "SEQ_PORT" |> int
        { Host = host
          Port = port
          Url = sprintf "http://%s:%d" host port }

type ServerConfig =
    { Host: string
      Port: int } with
    static member Load() =
        let host = Some "0.0.0.0" |> Env.getEnv "SERVER_HOST"
        let port = Some "4000" |> Env.getEnv "SERVER_PORT" |> int
        { Host = host
          Port = port }

type Config =
    { Server: ServerConfig
      LeaseApi: LeaseApiConfig
      Seq: SeqConfig } with
    static member Load() =
        { Server = ServerConfig.Load()
          LeaseApi = LeaseApiConfig.Load()
          Seq = SeqConfig.Load() }
