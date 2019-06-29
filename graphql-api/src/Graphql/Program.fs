open FSharp.Data.GraphQL
open Graphql
open Graphql.Root
open Graphql.JsonConverters
open Grpc.Core
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Suave
open Suave.Operators
open System

module JsonHelpers =
    let initJsonSerializerSettings () =
        let settings = JsonSerializerSettings()
        settings.Converters <- [| OptionConverter() |]
        settings.ContractResolver <- CamelCasePropertyNamesContractResolver()
        settings
    let serialize (settings:JsonSerializerSettings) (o:obj) = JsonConvert.SerializeObject(o, settings)
    let deserialize<'T> (s:string) = JsonConvert.DeserializeObject<'T>(s)

let tryParse fieldName (data:byte[]) =
    let raw = Text.Encoding.UTF8.GetString data
    if not (String.IsNullOrWhiteSpace(raw)) then
        JsonHelpers.deserialize<Map<string,obj>> raw
        |> Map.tryFind fieldName
    else None

let removeWhitespacesAndLineBreaks (str : string) = 
    str.Trim().Replace("\r\n", " ")

let getResponseContent (jsonSettings:JsonSerializerSettings) (res:Execution.GQLResponse) =
    match res.Content with
    | Execution.Direct (data, errors) ->
        match errors with
        | [] -> JsonConvert.SerializeObject(data, jsonSettings)
        | errors ->
            errors
            |> List.map fst
            |> String.concat ";"
            |> failwithf "Errors:\n%s"
    | _ -> failwithf "Only direct queries are supported!"

let graphql
    (jsonSettings:JsonSerializerSettings)
    (executor:Executor<Root.Root>) 
    : WebPart =
    fun httpCtx ->
        async {
            try
                let body = httpCtx.request.rawForm
                let query = 
                    body 
                    |> tryParse "query"
                    |> Option.map (fun o -> o.ToString())
                let variables = 
                    body 
                    |> tryParse "variables"
                    |> Option.map (fun o -> o.ToString())
                    |> Option.map JsonHelpers.deserialize<Map<string, obj>>
                match query, variables with
                | Some qry, Some variables ->
                    let formattedQry = removeWhitespacesAndLineBreaks qry
                    let root = { _empty = None }
                    let! gqlRes = executor.AsyncExecute(formattedQry, root, variables)
                    let res = getResponseContent jsonSettings gqlRes |> Successful.OK
                    return! httpCtx |> res
                | Some qry, None ->
                    let formattedQry = removeWhitespacesAndLineBreaks qry
                    let! gqlRes = executor.AsyncExecute(formattedQry)
                    let res = getResponseContent jsonSettings gqlRes |> Successful.OK
                    return! httpCtx |> res
                | None, _ ->
                    let! gqlRes = executor.AsyncExecute(Introspection.IntrospectionQuery)
                    let res = getResponseContent jsonSettings gqlRes |> Successful.OK
                    return! httpCtx |> res
            with ex ->
                let res =
                    {| data = ex.ToString() |}
                    |> JsonHelpers.serialize jsonSettings
                    |> ServerErrors.INTERNAL_ERROR
                return! httpCtx |> res
        }

let setCorsHeaders = 
    Writers.setHeader  "Access-Control-Allow-Origin" "*"
    >=> Writers.setHeader "Access-Control-Allow-Headers" "Content-Type"
    >=> Writers.addHeader "Access-Control-Allow-Headers" "X-Apollo-Tracing"
    >=> Writers.setHeader "Content-Type" "application/json"

[<EntryPoint>]
let main _ =
    let config = Config.load()
    let leaseChannel = Channel(config.LeaseApi.ChannelTarget, ChannelCredentials.Insecure)
    let leaseAPIClient = Tutorial.Lease.V1.LeaseAPI.LeaseAPIClient(leaseChannel)
    let leaseClient = Lease.LeaseClient(leaseAPIClient)
    let query = Query leaseClient
    let mutation = Mutation leaseClient
    let schema = Schema(query, mutation)
    let executor = Executor(schema)
    let jsonSettings = JsonHelpers.initJsonSerializerSettings()
    let suaveConfig =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" config.Port ]}
    let api = setCorsHeaders >=> graphql jsonSettings executor >=> Writers.setMimeType "application/json"
    startWebServer suaveConfig api
    leaseChannel.ShutdownAsync().Wait()
    0
