module GraphqlApi.HttpHandlers

open FSharp.Control.Tasks.V2.ContextInsensitive
open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open FSharp.UMX
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers
open Serilog

type HttpHandler = HttpFunc -> HttpContext -> HttpFuncResult

let setContentTypeAsJson : HttpHandler =
    setHttpHeader HeaderNames.ContentType "application/json"

let introspection
    (handler:GraphQLQueryHandler<'R>): HttpHandler =
    fun next ctx ->
        task {
            let! gqlResp =
                handler.ExecuteAsync(Introspection.IntrospectionQuery)
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
    (handler:GraphQLQueryHandler<'R>) : HttpHandler =
    fun next ctx ->
        task {
            try
                // NB: we don't use ctx.BindJsonAsync() because the deserialization is specifc to the executor
                let! body = ctx.ReadBodyFromRequestAsync()
                let query = handler.Deserialize<GraphQLQuery>(body)
                let permissions =
                    ctx.User.Claims
                    |> Seq.choose (fun claim ->
                        if claim.Type = "permissions" then Some claim.Value
                        else None)
                    |> Seq.toList
                let userId =
                    match ctx.User.Identity.Name with
                    | s when isNull s -> "anonymous"
                    | s -> s
                    |> UMX.tag<userId>
                let user =
                    { UserId = userId
                      Permissions = permissions }
                let meta = ["user", box user] |> Metadata.FromList
                let query = { query with ExecutionPlan = { query.ExecutionPlan with Metadata = meta } }
                let! gqlResp = 
                    handler.ExecuteAsync(
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

let app
    (handler:GraphQLQueryHandler<Root.Root>) =
    choose [
        route "/graphiql" >=> htmlFile "./graphiql/private.html"
        route "/schema" >=> choose [
            POST >=> introspection handler >=> setContentTypeAsJson
        ]
        route "/" >=> choose [
            GET >=> introspection handler >=> setContentTypeAsJson
            POST >=> graphql handler >=> setContentTypeAsJson
        ]
        route "/healthz" >=> GET >=> Successful.OK "Healthy!"
        RequestErrors.NOT_FOUND "location not available"
    ]