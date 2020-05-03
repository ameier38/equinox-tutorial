namespace Server

type VehicleClientConfig =
    { Url: string } with
    static member Load() =
        let host = Some "localhost" |> Shared.Env.getEnv "VEHICLE_API_HOST"
        let port = Some "50051" |> Shared.Env.getEnv "VEHICLE_API_PORT" |> int
        { Url = sprintf "%s:%d" host port }

type SeqConfig =
    { Url: string } with
    static member Load() =
        let host = Some "localhost" |> Shared.Env.getEnv "SEQ_HOST"
        let port = Some "5341" |> Shared.Env.getEnv "SEQ_PORT" |> int
        { Url = sprintf "http://%s:%d" host port }

type ServerConfig =
    { Host: string
      Port: int } with
    static member Load() =
        let host = Some "0.0.0.0" |> Shared.Env.getEnv "SERVER_HOST"
        let port = Some "4000" |> Shared.Env.getEnv "SERVER_PORT" |> int
        { Host = host
          Port = port }

type Config =
    { AppName: string
      Debug: bool
      Server: ServerConfig
      VehicleClient: VehicleClientConfig
      Seq: SeqConfig } with
    static member Load() =
        { AppName = "GraphqlApi"
          Debug = Some "true" |> Shared.Env.getEnv "DEBUG" |> bool.Parse
          Server = ServerConfig.Load()
          VehicleClient = VehicleClientConfig.Load()
          Seq = SeqConfig.Load() }
