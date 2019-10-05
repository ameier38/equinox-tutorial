namespace Server

type SeqConfig =
    { Host: string
      Port: int
      Url: string } with
    static member Load() =
        let host = Some "localhost" |> Shared.Env.getEnv "SEQ_HOST"
        let port = Some "5341" |> Shared.Env.getEnv "SEQ_PORT" |> int
        { Host = host
          Port = port
          Url = sprintf "http://%s:%d" host port }

type ServerConfig =
    { Host: string
      Port: int } with
    static member Load() =
        let host = Some "0.0.0.0" |> Shared.Env.getEnv "SERVER_HOST"
        let port = Some "4000" |> Shared.Env.getEnv "SERVER_PORT" |> int
        { Host = host
          Port = port }

type Config =
    { Server: ServerConfig
      LeaseApi: Lease.LeaseApiConfig
      Seq: SeqConfig } with
    static member Load() =
        { Server = ServerConfig.Load()
          LeaseApi = Lease.LeaseApiConfig.Load()
          Seq = SeqConfig.Load() }
