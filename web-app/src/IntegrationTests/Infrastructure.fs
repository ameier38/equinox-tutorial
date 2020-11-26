[<AutoOpen>]
module Infrastructure

open System

module Env =
    let getEnv (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrEmpty(value) -> defaultValue
        | value -> value

module Url =
    let getPortComponent (port:string) =
        match port with
        | "" | "80" | "443" -> ""
        | port -> ":" + port
