open FSharp.Data
open FSharp.UMX
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Server
open Serilog
open Serilog.Events
open Suave
open Suave.Filters
open Suave.Operators
open System.Text
open System.IdentityModel.Tokens

type Parser<'T>(executor:GraphQL.Executor<'T>) =
    let jsonOptions = JsonSerializerSettings(ContractResolver = CamelCasePropertyNamesContractResolver())
    do jsonOptions.Converters.Add(GraphQLQueryConverter(executor))
    do jsonOptions.Converters.Add(OptionConverter())

    // ref: https://graphql.org/learn/serving-over-http/#post-request
    member _.ParseRequest(user:User, rawBody:byte[]) =
        let strBody = Encoding.UTF8.GetString(rawBody)
        Log.Debug("received request {@Request}", strBody)
        let query = JsonConvert.DeserializeObject<GraphQLQuery>(strBody, jsonOptions)
        let meta = ["user", box user] |> GraphQL.Types.Metadata.FromList
        { query with ExecutionPlan = { query.ExecutionPlan with Metadata = meta } }

    // ref: https://graphql.org/learn/serving-over-http/#response
    member _.ParseResponse(res:GraphQL.Execution.GQLResponse) =
        match res.Content with
        | GraphQL.Execution.Direct (data, errors) ->
            match errors with
            | [] -> JsonConvert.SerializeObject(data)
            | errors -> failwithf "%A" errors
        | _ -> failwithf "only direct queries are supported"

type TokenParser() =
    let tokenHandler = Jwt.JwtSecurityTokenHandler()

    member _.ParseToken(bearer:string) =
        match bearer with
        | Regex.Match "^Bearer (.+)$" [token] ->
            let parsedToken = tokenHandler.ReadJwtToken(token)
            let permissions =
                parsedToken.Claims
                |> Seq.choose (fun claim ->
                    if claim.Type = "permissions" then Some claim.Value
                    else None)
                |> Seq.toList
            let userId = UMX.tag<userId> parsedToken.Subject
            { UserId = userId; Permissions = permissions }
        | _ -> failwithf "could not parse token"


let setCORSHeaders =
    Writers.setHeader  "Access-Control-Allow-Origin" "*"
    >=> Writers.setHeader "Access-Control-Allow-Headers" "Content-Type"

let setResponseHeaders = 
    setCORSHeaders
    >=> Writers.setHeader "Content-Type" "application/json"
    >=> Writers.setMimeType "application/json"

let authorize
    (parser:TokenParser): WebPart =
    context (fun ctx ->
        try
            match ctx.request.header "authorization" with
            | Choice1Of2 bearer ->
                let user = parser.ParseToken(bearer)
                Writers.setUserData "user" user
            | Choice2Of2 msg ->
                sprintf "could not find authorization header: %s" msg
                |> RequestErrors.UNAUTHORIZED
        with ex ->
            sprintf "failed to parse authorization header %A" ex
            |> RequestErrors.UNAUTHORIZED
    )

let introspection
    (parser:Parser<Root.Root>)
    (executor:GraphQL.Executor<Root.Root>): WebPart =
    fun httpCtx ->
        async {
            let! gqlResp = executor.AsyncExecute(GraphQL.Introspection.IntrospectionQuery)
            let sendResp = 
                parser.ParseResponse(gqlResp)
                |> Successful.OK
            return! sendResp httpCtx
        }

let graphql 
    (parser:Parser<Root.Root>)
    (executor:GraphQL.Executor<Root.Root>): WebPart =
    fun httpCtx ->
        async {
            try
                match httpCtx.request.rawForm with
                | [||] ->
                    let sendResp = RequestErrors.BAD_REQUEST "empty request"
                    return! sendResp httpCtx
                | body ->
                    let user =
                        match httpCtx.userState.TryFind "user" with
                        | Some o ->
                            match o with
                            | :? User as user -> user
                            | _ -> failwithf "could not parse user"
                        | None -> failwithf "user key not found"
                    Log.Debug("User {@User}", user)
                    Log.Debug("Parsing request")
                    let query = parser.ParseRequest(user, body)
                    Log.Debug("Executing request")
                    let! gqlResp = 
                        executor.AsyncExecute(
                            executionPlan = query.ExecutionPlan,
                            variables = query.Variables)
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
            .WriteTo.Seq(config.SeqConfig.Url)
            .CreateLogger()
    Log.Logger <- logger
    let vehicleClient = VehicleClient(config.VehicleApiConfig, config.MongoConfig)
    let query = Root.Query vehicleClient
    let mutation = Root.Mutation vehicleClient
    let schema = GraphQL.Schema(query, mutation)
    let executor = GraphQL.Executor(schema)
    let tokenParser = TokenParser()
    let parser = Parser(executor)
    let suaveConfig = 
        { defaultConfig 
            with bindings = [ HttpBinding.createSimple HTTP config.ServerConfig.Host config.ServerConfig.Port ]}
    let api = choose [
        path "/introspection" >=> choose [
            OPTIONS >=> setCORSHeaders >=> Successful.OK "CORS approved"
            POST >=> introspection parser executor >=> setResponseHeaders
        ]
        path "/" >=> choose [
            OPTIONS >=> setCORSHeaders >=> Successful.OK "CORS approved"
            GET >=> introspection parser executor >=> setResponseHeaders
            POST >=> authorize tokenParser >=> graphql parser executor >=> setResponseHeaders
        ]
        path "/_health" >=> Successful.OK "Healthy!"
        RequestErrors.NOT_FOUND "location not available"
    ]
    Log.Information("🚀 Server listening at :{Port}", config.ServerConfig.Port)
    Log.Information("📜 Logs sent to {Url}", config.SeqConfig.Url)
    startWebServer suaveConfig api
    0
