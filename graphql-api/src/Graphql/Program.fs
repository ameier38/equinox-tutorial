open FSharp.Data.GraphQL
open Graphql
open Grpc.Core
open Newtonsoft.Json
open Suave
open Suave.Operators
open System

let settings = JsonSerializerSettings()
settings.ContractResolver <- Serialization.CamelCasePropertyNamesContractResolver()
let json o = JsonConvert.SerializeObject(o, settings)
    
let tryParseRequest (data:byte array) =
    let raw = Text.Encoding.UTF8.GetString data
    if not (raw |> isNull) && raw <> "" then
        let map = JsonConvert.DeserializeObject<Map<string,obj>>(raw)
        Map.tryFind "query" map 
        |> Option.map (fun qry -> qry :?> string)
    else None

let getResponseContent (res: Execution.GQLResponse) =
    match res.Content with
    | Execution.Direct (data, errors) ->
        match errors with
        | [] -> data |> json
        | errors ->
            errors
            |> List.map fst
            |> String.concat ";"
            |> failwithf "Errors:\n%s"
    | _ -> failwithf "Only direct queries are supported!"

let graphql
    (executor:Executor<Root.Root>) 
    : WebPart =
    fun httpCtx ->
        async {
            try
                match tryParseRequest httpCtx.request.rawForm with
                | Some query ->
                    let! gqlRes = executor.AsyncExecute(query)
                    let res = getResponseContent gqlRes |> Successful.OK
                    return! httpCtx |> res
                | None ->
                    let! gqlRes = executor.AsyncExecute(Introspection.IntrospectionQuery)
                    let res = getResponseContent gqlRes |> Successful.OK
                    return! httpCtx |> res
            with ex ->
                let res =
                    {| data = ex.ToString() |}
                    |> json
                    |> ServerErrors.INTERNAL_ERROR
                return! httpCtx |> res
        }

let setCorsHeaders = 
    Writers.setHeader  "Access-Control-Allow-Origin" "*"
    >=> Writers.setHeader "Access-Control-Allow-Headers" "Content-Type"
    >=> Writers.addHeader "Access-Control-Allow-Headers" "X-Apollo-Tracing"

[<EntryPoint>]
let main _ =
    let config = Config.load()
    let leaseChannel = Channel(config.LeaseApi.ChannelTarget, ChannelCredentials.Insecure)
    let leaseAPIClient = Tutorial.Lease.V1.LeaseAPI.LeaseAPIClient(leaseChannel)
    let leaseClient = Lease.LeaseClient(leaseAPIClient)
    let query = Root.Query leaseClient
    let mutation = Root.Mutation leaseClient
    let schema = Schema(query, mutation)
    let executor = Executor(schema)
    let suaveConfig =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" config.Port ]}
    let api = setCorsHeaders >=> graphql executor >=> Writers.setMimeType "application/json"
    startWebServer suaveConfig api
    leaseChannel.ShutdownAsync().Wait()
    0