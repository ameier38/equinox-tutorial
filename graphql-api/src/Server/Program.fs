open FSharp.Data
open Grpc.Core
open Server
open Serilog
open Shared
open Suave
open Suave.Filters
open Suave.Operators

let tryParseBody (rawBody:byte[]) =
    let sBody = rawBody |> String.fromBytes
    if sBody |> String.isNullOrWhiteSpace then None
    else
        rawBody
        |> String.fromBytes
        |> JsonHelpers.deserialize<Map<string,obj>>
        |> Some

let tryParseQuery (schema:GraphQL.Schema<Root.Root>) (body:Map<string,obj>) =
    if body.ContainsKey("query") then
        match body.["query"] with
        | :? string as query -> 
            let varDefs = Variables.parseVariableDefinitions query
            Some (query, Variables.getVariables schema varDefs body)
        | _ -> failwith "Failure deserializing repsonse. Could not read query - it is not stringified in request."
    else None

let removeWhitespacesAndLineBreaks (str : string) = 
    str.Trim().Replace("\r\n", " ")

let getResponseContent (res:GraphQL.Execution.GQLResponse) =
    match res.Content with
    | GraphQL.Execution.Direct (data, errors) ->
        match errors with
        | [] -> JsonHelpers.serialize data
        | errors ->
            errors
            |> List.map fst
            |> String.concat ";"
            |> failwithf "Errors:\n%s"
    | _ -> failwithf "Only direct queries are supported!"

let graphql
    (logger:Core.Logger)
    (schema:GraphQL.Schema<Root.Root>) 
    : WebPart =
    fun httpCtx ->
        async {
            try
                let executor = GraphQL.Executor(schema)
                let body = 
                    httpCtx.request.rawForm 
                    |> tryParseBody
                let query = 
                    body
                    |> Option.bind (tryParseQuery schema)
                match query with
                | Some (qry, Some variables) ->
                    logger.Information("request query {Query}", qry)
                    logger.Information(sprintf "request variables {Variables}", variables)
                    let formattedQry = removeWhitespacesAndLineBreaks qry
                    let root = { Root._empty = None }
                    let! gqlRes = executor.AsyncExecute(formattedQry, root, variables)
                    let res = getResponseContent gqlRes |> Successful.OK
                    return! httpCtx |> res
                | Some (qry, None) ->
                    logger.Information("request query {Query}", qry)
                    let formattedQry = removeWhitespacesAndLineBreaks qry
                    let! gqlRes = executor.AsyncExecute(formattedQry)
                    let res = getResponseContent gqlRes |> Successful.OK
                    return! httpCtx |> res
                | None ->
                    let! gqlRes = executor.AsyncExecute(GraphQL.Introspection.IntrospectionQuery)
                    let res = getResponseContent gqlRes |> Successful.OK
                    return! httpCtx |> res
            with ex ->
                let res =
                    {| data = ex.ToString() |}
                    |> JsonHelpers.serialize
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
    let config = Config.Load()
    let logger = 
        LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Seq(config.Seq.Url)
            .CreateLogger()
    let leaseChannel = Channel(config.LeaseApi.ChannelTarget, ChannelCredentials.Insecure)
    let leaseAPIClient = Tutorial.Lease.V1.LeaseAPI.LeaseAPIClient(leaseChannel)
    let leaseClient = Lease.Client.LeaseClient(leaseAPIClient, logger)
    let query = Root.Query leaseClient
    let mutation = Root.Mutation leaseClient
    let schema = GraphQL.Schema(query, mutation)
    let executor = GraphQL.Executor(schema)
    let suaveConfig =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP config.Server.Host config.Server.Port ]}
    let api = choose [ 
        path "/health" >=> Successful.OK "Hello" 
        setCorsHeaders >=> graphql logger schema >=> Writers.setMimeType "application/json" 
    ]
    logger.Information(sprintf "logging at %s 📝" config.Seq.Url)
    logger.Information("starting GraphQL API 🚀")
    startWebServer suaveConfig api
    leaseChannel.ShutdownAsync().Wait()
    0
