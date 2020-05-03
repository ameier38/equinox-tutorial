open FSharp.Data
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Server
open Serilog
open Serilog.Events
open Suave
open Suave.Filters
open Suave.Operators
open System.Text

type Parser<'T>(executor:GraphQL.Executor<'T>) =
    let jsonOptions = JsonSerializerSettings(ContractResolver = CamelCasePropertyNamesContractResolver())
    do jsonOptions.Converters.Add(GraphQLQueryConverter(executor))
    do jsonOptions.Converters.Add(OptionConverter())

    // ref: https://graphql.org/learn/serving-over-http/#post-request
    member _.ParseRequest(rawBody:byte[]) =
        let strBody = Encoding.UTF8.GetString(rawBody)
        Log.Debug("received request {@Request}", strBody)
        JsonConvert.DeserializeObject<GraphQLQuery>(strBody, jsonOptions)

    // ref: https://graphql.org/learn/serving-over-http/#response
    member _.ParseResponse(res:GraphQL.Execution.GQLResponse) =
        match res.Content with
        | GraphQL.Execution.Direct (data, errors) ->
            match errors with
            | [] -> JsonConvert.SerializeObject(data)
            | errors -> failwithf "%A" errors
        | _ -> failwithf "only direct queries are supported"

let setCORSHeaders =
    Writers.setHeader  "Access-Control-Allow-Origin" "*"
    >=> Writers.setHeader "Access-Control-Allow-Headers" "Content-Type"

let setResponseHeaders = 
    setCORSHeaders
    >=> Writers.setHeader "Content-Type" "application/json"
    >=> Writers.setMimeType "application/json"

let graphql 
    (parser:Parser<Root.Root>)
    (executor:GraphQL.Executor<Root.Root>): WebPart =
    fun httpCtx ->
        async {
            try
                match httpCtx.request.rawForm with
                | [||] ->
                    let sendResp =
                        {| data = "" |}
                        |> JsonConvert.SerializeObject
                        |> Successful.OK
                    return! sendResp httpCtx
                | body ->
                    Log.Debug("Parsing request")
                    let { ExecutionPlan = plan; Variables = variables } = parser.ParseRequest(body)
                    Log.Debug("Executing request")
                    let! gqlResp = 
                        executor.AsyncExecute(
                            executionPlan=plan,
                            variables=variables)
                    Log.Debug("Parsing response")
                    let sendResp = 
                        parser.ParseResponse(gqlResp)
                        |> Successful.OK
                    return! sendResp httpCtx
            with ex ->
                Log.Error("Error: {@Exception}", ex)
                let sendResp =
                    {| data = ex.ToString() |}
                    |> JsonConvert.SerializeObject
                    |> ServerErrors.INTERNAL_ERROR
                return! sendResp httpCtx
        }

[<EntryPoint>]
let main _ =
    let config = Config.Load()
    let logger = 
        LoggerConfiguration()
            .Enrich.WithProperty("Application", config.AppName)
            .MinimumLevel.Is(if config.Debug then LogEventLevel.Debug else LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.Seq(config.Seq.Url)
            .CreateLogger()
    Log.Logger <- logger
    let vehicleClient = VehicleClient(config.VehicleClient)
    let query = Root.Query vehicleClient
    let mutation = Root.Mutation vehicleClient
    let schema = GraphQL.Schema(query, mutation)
    let executor = GraphQL.Executor(schema)
    let parser = Parser(executor)
    let suaveConfig = 
        { defaultConfig 
            with bindings = [ HttpBinding.createSimple HTTP config.Server.Host config.Server.Port ]}
    let api = choose [
        OPTIONS >=> setCORSHeaders >=> Successful.OK "CORS approved"
        POST >=> graphql parser executor >=> setResponseHeaders
        GET >=> path "/_health" >=> Successful.OK "Healthy!"
        RequestErrors.NOT_FOUND "location not available"
    ]
    Log.Information("🚀 Server listening at :{Port}", config.Server.Port)
    Log.Information("🔗 Connected to Vehicle API at {Url}", config.VehicleClient.Url)
    Log.Information("📜 Logs sent to {Url}", config.Seq.Url)
    startWebServer suaveConfig api
    0
