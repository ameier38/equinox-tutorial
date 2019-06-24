namespace Lease

open System
open FsConfig

module Env =
    let getEnv name = Environment.GetEnvironmentVariable name
    let getEnvOrDefault name defaultValue =
        match getEnv name with
        | value when String.IsNullOrEmpty value -> defaultValue
        | value -> value

[<Convention("EVENTSTORE")>]
type EventStoreConfig = {
    Host: string
    Port: int
    User: string
    Password: string

}
type Config =
    {| Debug: bool
       Port: int
       EventStore: 
        {| Uri: Uri
           User: string
           Password: string |} |}
module Config =
    let load () =
        let! debug = getEnv "DEBUG" "false"
        let! port = Some "50051" |> getEnv "PORT"
        let! eventStoreProtocol = Some "tcp" |> getEnv "EVENTSTORE_PROTOCOL"
        let! eventStoreHost = Some "localhost" |> getEnv "EVENTSTORE_HOST"
        let! eventStorePort = Some "1113" |> getEnv "EVENTSTORE_PORT"
        let! eventStoreUser = Some "admin" |> getEnv "EVENTSTORE_USER"
        let! eventStorePassword = Some "changeit" |> getEnv "EVENTSTORE_PASSWORD"
        let eventStoreUri = 
            sprintf "%s://@%s:%s" 
                eventStoreProtocol
                eventStoreHost
                eventStorePort
            |> Uri
        {| Debug = debug
           Port = port |> int
           EventStore = 
            {| Uri = eventStoreUri
               User = eventStoreUser 
               Password = eventStorePassword |} |}
