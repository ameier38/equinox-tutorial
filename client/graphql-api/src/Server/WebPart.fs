module Server.WebPart

open FSharp.Data.GraphQL
open FSharp.UMX
open Newtonsoft.Json
open Serilog
open Suave
open Suave.Operators

let setCORSHeaders: WebPart =
    Writers.setHeader "Access-Control-Allow-Origin" "*"
    >=> Writers.setHeader "Access-Control-Allow-Headers" "Content-Type,Authorization"

let setResponseHeaders: WebPart = 
    Writers.setHeader "Content-Type" "application/json"
    >=> Writers.setMimeType "application/json"

let authorize
    (tokenParser:TokenParser): WebPart =
    context (fun ctx ->
        try
            match ctx.request.header "authorization" with
            | Choice1Of2 bearer ->
                let user = tokenParser.ParseToken(bearer)
                Writers.setUserData "user" user
            | Choice2Of2 msg ->
                sprintf "could not find authorization header: %s" msg
                |> RequestErrors.UNAUTHORIZED
        with ex ->
            sprintf "failed to parse authorization header %A" ex
            |> RequestErrors.UNAUTHORIZED
    )

let introspection
    (graphqlParser:GraphQLParser<Root.Root>): WebPart =
    fun httpCtx ->
        async {
            let! gqlResp = graphqlParser.Executor.AsyncExecute(Introspection.IntrospectionQuery)
            let sendResp = 
                graphqlParser.ParseResponse(gqlResp)
                |> Successful.OK
            return! sendResp httpCtx
        }

let graphql 
    (parser:GraphQLParser<Root.Root>): WebPart =
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
                        | None ->
                            { UserId = %"public"; Permissions = []}
                    Log.Debug("User {@User}", user)
                    Log.Debug("Parsing request")
                    let query = parser.ParseRequest(user, body)
                    Log.Debug("Executing request")
                    let! gqlResp = 
                        parser.Executor.AsyncExecute(
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
