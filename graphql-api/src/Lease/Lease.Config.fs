namespace Lease

type LeaseApiConfig =
    { Host: string
      Port: int
      ChannelTarget: string } with
    static member Load() =
        let host = Some "localhost" |> Shared.Env.getEnv "LEASE_API_HOST"
        let port = Some "50051" |> Shared.Env.getEnv "LEASE_API_PORT" |> int
        { Host = host
          Port = port
          ChannelTarget = sprintf "%s:%d" host port }
