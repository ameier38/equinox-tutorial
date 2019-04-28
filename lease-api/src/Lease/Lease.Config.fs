namespace Lease

open Env
open System

type Config =
    {| Debug: bool
       Port: int
       EventStore: 
        {| Uri: Uri
           User: string
           Password: string |} |}
module Config =
    let load () =
        result {
            let! debug = Some "false" |> getEnv "DEBUG" |> Result.map Boolean.Parse
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
            let config : Config =
                {| Debug = debug
                   Port = port |> int
                   EventStore = 
                    {| Uri = eventStoreUri
                       User = eventStoreUser 
                       Password = eventStorePassword |} |}
            return config
        } |> Result.bimap id (failwithf "Error!:\n%A")
