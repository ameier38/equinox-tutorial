module GraphqlApi.HttpHandlers

open FSharp.Control.Tasks.V2.ContextInsensitive
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.UMX
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.Net.Http.Headers
open Serilog
open System.Threading.Tasks

type HttpHandler = HttpFunc -> HttpContext -> HttpFuncResult

let setContentTypeAsJson : HttpHandler =
    setHttpHeader HeaderNames.ContentType "application/json"

let mustBeLoggedIn: HttpHandler =
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let noop : HttpHandler = handleContext(Some >> Task.FromResult)

let introspection
    (executor:Executor<Root.Root>): HttpHandler =
    fun next ctx ->
        task {
            let! gqlResp =
                executor.AsyncExecute(Introspection.IntrospectionQuery)
                |> Async.StartAsTask
            let res =
                match gqlResp.Content with
                | Execution.Direct (data, errors) ->
                    match errors with
                    | [] ->
                        Successful.OK data 
                    | errors ->
                        ServerErrors.INTERNAL_ERROR errors
                | _ ->
                    ServerErrors.INTERNAL_ERROR "only direct queries are supported"
            return! res next ctx
        }

let graphql 
    (executor:Executor<Root.Root>): HttpHandler =
    fun next ctx ->
        task {
            try
                let! query = ctx.BindJsonAsync<GraphQLQuery>()
                let permissions =
                    ctx.User.Claims
                    |> Seq.choose (fun claim ->
                        if claim.Type = "permissions" then Some claim.Value
                        else None)
                    |> Seq.toList
                let userId =
                    match ctx.User.Identity.Name with
                    | s when isNull s -> ""
                    | s -> s
                    |> UMX.tag<userId>
                let user =
                    { UserId = userId
                      Permissions = permissions }
                let meta = ["user", box user] |> Metadata.FromList
                let query = { query with ExecutionPlan = { query.ExecutionPlan with Metadata = meta } }
                let! gqlResp = 
                    executor.AsyncExecute(
                        executionPlan = query.ExecutionPlan,
                        variables = query.Variables)
                let res =
                    match gqlResp.Content with
                    | Execution.Direct (data, errors) ->
                        match errors with
                        | [] ->
                            Successful.OK data 
                        | errors ->
                            ServerErrors.INTERNAL_ERROR errors
                    | _ ->
                        ServerErrors.INTERNAL_ERROR "only direct queries are supported"
                return! res next ctx
            with ex ->
                Log.Error("Error: {@Exception}", ex)
                let res =
                    {| data = ex.ToString() |}
                    |> ServerErrors.INTERNAL_ERROR
                return! res next ctx
        }

let app (executor:Executor<Root.Root>) (authenticate:bool) =
    choose [
        if authenticate then mustBeLoggedIn else noop >=> choose [
            route "/schema" >=> choose [
                POST >=> introspection executor >=> setContentTypeAsJson
            ]
            route "/" >=> choose [
                GET >=> introspection executor >=> setContentTypeAsJson
                POST >=> graphql executor >=> setContentTypeAsJson
            ]
        ] 
        route "/_health" >=> Successful.OK "Healthy!"
        RequestErrors.NOT_FOUND "location not available"
    ]
