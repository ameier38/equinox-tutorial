open Argu
open FSharp.Data
open Giraffe
open Giraffe.Serialization
open GraphqlApi
open GraphqlApi.Vehicle.Client
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.IdentityModel.Tokens
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Serilog
open Serilog.Events
open System.Text

type ApiAudience =
    | Public
    | Private

type Arguments =
    | [<Mandatory>] Audience of ApiAudience
    | Insecure
    interface IArgParserTemplate with
        member a.Usage =
            match a with
            | Audience _ -> "Audience for API; either public or private"
            | Insecure -> "Remove authentication"

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "graphql-api")
    let parsedArgs = parser.ParseCommandLine(argv)
    let apiAudience = parsedArgs.GetResult Audience
    let insecure = parsedArgs.TryGetResult Insecure |> Option.isSome
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
    let schema, authenticate =
        match apiAudience with
        | Private ->
            let query = Root.PrivateQuery vehicleClient
            let mutation = Root.PrivateMutation vehicleClient
            GraphQL.Schema(query, mutation), not insecure
        | Public ->
            let query = Root.PublicQuery vehicleClient
            GraphQL.Schema(query), false
    let executor = GraphQL.Executor(schema)
    let app = HttpHandlers.app executor authenticate

    let configureApp (application: IApplicationBuilder) =
        application
            .UseAuthentication()
            .UseGiraffe(app)

    let configureServices (services: IServiceCollection) =
        let jsonOptions = JsonSerializerSettings(ContractResolver = CamelCasePropertyNamesContractResolver())
        jsonOptions.Converters.Add(JsonConverter.GraphQLQueryConverter(executor))
        jsonOptions.Converters.Add(JsonConverter.OptionConverter())
        services
            .AddGiraffe()
            .AddCors()
            .AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer(jsonOptions))
            .AddAuthentication(fun opts ->
                opts.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                opts.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(fun opts ->
                let signingKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Auth0Config.Secret))
                let tokenValidationParams =
                    TokenValidationParameters(
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = config.Auth0Config.Issuer,
                        ValidAudience = config.Auth0Config.Audience,
                        IssuerSigningKey = signingKey
                    )
                opts.TokenValidationParameters <- tokenValidationParams)
         |> ignore

    Log.Information("🚀 Server listening at {Host}:{Port}", config.ServerConfig.Host, config.ServerConfig.Port)
    Log.Information("📜 Logs sent to {Url}", config.SeqConfig.Url)
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .Configure(configureApp)
                .ConfigureServices(configureServices)
                .UseUrls(sprintf "http://%s:%i" config.ServerConfig.Host config.ServerConfig.Port)
                |> ignore)
        .Build()
        .Run()
    0
