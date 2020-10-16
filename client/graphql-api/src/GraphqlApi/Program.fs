open FSharp.Data
open Giraffe
open GraphqlApi
open GraphqlApi.Vehicle.Client
open Grpc.Core
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.IdentityModel.Tokens
open Serilog
open Serilog.Events
open System.Text

[<EntryPoint>]
let main argv =
    let config = Config.Load()
    let logger = 
        LoggerConfiguration()
            .Enrich.WithProperty("Application", config.AppName)
            .MinimumLevel.Is(if config.Debug then LogEventLevel.Debug else LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.Seq(config.SeqConfig.Url)
            .CreateLogger()
    Log.Logger <- logger
    Log.Debug("🐛 Debug mode")
    let vehicleCommandChannel = Channel(config.VehicleProcessorConfig.Url, ChannelCredentials.Insecure)
    let vehicleCommandClient = CosmicDealership.Vehicle.V1.VehicleCommandService.VehicleCommandServiceClient(vehicleCommandChannel)
    let vehicleQueryChannel = Channel(config.VehicleReaderConfig.Url, ChannelCredentials.Insecure)
    let vehicleQueryClient = CosmicDealership.Vehicle.V1.VehicleQueryService.VehicleQueryServiceClient(vehicleQueryChannel)
    let vehicleClient = VehicleClient(vehicleCommandClient, vehicleQueryClient)
    let publicHandler =
        let query = Root.PublicQuery vehicleClient
        let schema = GraphQL.Schema(query)
        let executor = GraphQL.Executor(schema)
        GraphQLQueryHandler(executor)
    let privateHandler =
        let query = Root.PrivateQuery vehicleClient
        let mutation = Root.PrivateMutation vehicleClient
        let schema = GraphQL.Schema(query, mutation)
        let executor = GraphQL.Executor(schema)
        GraphQLQueryHandler(executor)

    let app = HttpHandlers.app publicHandler privateHandler

    let configureApp (application: IApplicationBuilder) =
        application
            .UseAuthentication()
            .UseCors(fun corsBuilder ->
                corsBuilder.AllowAnyHeader() |> ignore
                corsBuilder.AllowAnyMethod() |> ignore
                corsBuilder.AllowAnyOrigin() |> ignore)
            .UseGiraffe(app)

    let configureServices (services: IServiceCollection) =
        services
            .AddGiraffe()
            .AddCors()
            .AddAuthentication(fun opts ->
                opts.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                opts.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(fun opts ->
                let signingKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Auth0Config.ClientSecret))
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
