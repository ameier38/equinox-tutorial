namespace Lease

open Env
open System

type EventStoreConfig =
    { Protocol: string
      Host: string
      Port: int
      User: string
      Password: string }
module EventStoreConfig =
    let read key = 
        Environment.GetEnvironmentVariable key 
        |> Option.ofObj 
        |> Option.get
    let load () =
        result {
            let! protocol = Some "tcp" |> getEnv "EVENTSTORE_PROTOCOL"
            let! host = Some "localhost" |> getEnv "EVENTSTORE_HOST"
            let! port = Some "1113" |> getEnv "EVENTSTORE_PORT"
            let! user = Some "admin" |> getEnv "EVENTSTORE_USER"
            let! password = Some "changeit" |> getEnv "EVENTSTORE_PASSWORD"
            return
                { Protocol = protocol
                  Host = host
                  Port = port |> int 
                  User = user 
                  Password = password }
        } 
        |> function
           | Ok config -> config
           | Error err -> failwithf "Failed to load Event Store config:\n%s" err
