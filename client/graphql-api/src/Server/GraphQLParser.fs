namespace Server

open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Types
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.Text

type GraphQLParser<'T>(executor:Executor<'T>) =
    let jsonOptions = JsonSerializerSettings(ContractResolver = CamelCasePropertyNamesContractResolver())
    do jsonOptions.Converters.Add(JsonConverter.GraphQLQueryConverter(executor))
    do jsonOptions.Converters.Add(JsonConverter.OptionConverter())

    member _.Executor = executor

    // ref: https://graphql.org/learn/serving-over-http/#post-request
    member _.ParseRequest(user:User, rawBody:byte[]) =
        let strBody = Encoding.UTF8.GetString(rawBody)
        let query = JsonConvert.DeserializeObject<GraphQLQuery>(strBody, jsonOptions)
        let meta = ["user", box user] |> Metadata.FromList
        { query with ExecutionPlan = { query.ExecutionPlan with Metadata = meta } }

    // ref: https://graphql.org/learn/serving-over-http/#response
    member _.ParseResponse(res:Execution.GQLResponse) =
        match res.Content with
        | Execution.Direct (data, errors) ->
            match errors with
            | [] -> JsonConvert.SerializeObject(data)
            | errors -> failwithf "%A" errors
        | _ -> failwithf "only direct queries are supported"