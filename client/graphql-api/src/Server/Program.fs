open FSharp.Data
open Server
open Serilog
open Serilog.Events
open Server.Vehicle.Client
open Suave
open Suave.Filters
open Suave.Operators
open WebPart

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
    let publicQuery = Root.PublicQuery vehicleClient
    let mutation = Root.Mutation vehicleClient
    let schema = GraphQL.Schema(query, mutation)
    let publicSchema = GraphQL.Schema(publicQuery)
    let executor = GraphQL.Executor(schema)
    let publicExecutor = GraphQL.Executor(publicSchema)
    let tokenParser = TokenParser()
    let graphQLParser = GraphQLParser(executor)
    let publicGraphQLParser = GraphQLParser(publicExecutor)
    let suaveConfig = 
        { defaultConfig 
            with bindings = [ HttpBinding.createSimple HTTP config.ServerConfig.Host config.ServerConfig.Port ]}
    let api = choose [
        path "/public/schema" >=> choose [
            OPTIONS >=> setCORSHeaders >=> Successful.OK "CORS approved"
            POST >=> introspection publicGraphQLParser >=> setResponseHeaders
        ]
        path "/public" >=> choose [
            OPTIONS >=> setCORSHeaders >=> Successful.OK "CORS approved"
            GET >=> introspection publicGraphQLParser >=> setResponseHeaders
            POST >=> setCORSHeaders >=> graphql publicGraphQLParser >=> setResponseHeaders
        ]
        path "/schema" >=> choose [
            OPTIONS >=> setCORSHeaders >=> Successful.OK "CORS approved"
            POST >=> introspection graphQLParser >=> setResponseHeaders
        ]
        path "/" >=> choose [
            OPTIONS >=> setCORSHeaders >=> Successful.OK "CORS approved"
            GET >=> introspection graphQLParser >=> setResponseHeaders
            POST >=> setCORSHeaders >=> authorize tokenParser >=> graphql graphQLParser >=> setResponseHeaders
        ]
        path "/_health" >=> Successful.OK "Healthy!"
        RequestErrors.NOT_FOUND "location not available"
    ]
    Log.Information("🚀 Server listening at :{Port}", config.ServerConfig.Port)
    Log.Information("📜 Logs sent to {Url}", config.SeqConfig.Url)
    startWebServer suaveConfig api
    0
