open FSharp.Data.GraphQL
open FSharp.Data.GraphQL.Execution
open Graphql
open Grpc.Core
open Newtonsoft.Json
open Suave
open Suave.Operators
open System

let settings = JsonSerializerSettings()
settings.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
let json o = JsonConvert.SerializeObject(o, settings)
    
let tryParseRequest (data:byte array) =
    try
        let raw = Text.Encoding.UTF8.GetString data
        if not (raw |> isNull) && raw <> "" then
            let map = JsonConvert.DeserializeObject<Map<string,obj>>(raw)
            Map.tryFind "query" map 
            |> Option.map (fun qry -> qry :?> string)
            |> Ok
        else None |> Ok
    with ex -> sprintf "error parsing request %A" ex |> Error

let getResponse = function
    | Direct (data, errors) -> 
        if errors |> List.isEmpty then
            data |> json |> Ok
        else errors |> List.map (fun (e, _) -> e) |> String.concat ";" |> Error
    | _ -> Error "only direct queries are supported"

let graphql
    (executor:Executor<Root.Root>) 
    : WebPart =
    fun httpCtx ->
        async {
            let onSuccess response = Successful.OK response
            let onFailure err = RequestErrors.BAD_REQUEST err
            match tryParseRequest httpCtx.request.rawForm with
            | Ok (Some query) ->
                let! gqlResponse = executor.AsyncExecute(query)
                let response = getResponse gqlResponse |> Result.bimap onSuccess onFailure
                return! httpCtx |> response
            | Ok None ->
                let! gqlResponse = executor.AsyncExecute(Introspection.IntrospectionQuery)
                let response = getResponse gqlResponse |> Result.bimap onSuccess onFailure
                return! httpCtx |> response
            | Error err ->
                return! httpCtx |> onFailure err
        }

let setCorsHeaders = 
    Writers.setHeader  "Access-Control-Allow-Origin" "*"
    >=> Writers.setHeader "Access-Control-Allow-Headers" "content-type"

[<EntryPoint>]
let main _ =
    let config = Config.load()
    let membershipChannel = Channel(config.MembershipApi.Url, ChannelCredentials.Insecure)
    let membershipClient = Proto.Membership.MembershipService.MembershipServiceClient(membershipChannel)
    let membershipService = Membership.MembershipService(membershipClient)
    let query = Root.Query membershipService
    let mutation = Root.Mutation membershipService
    let schema = Schema(query, mutation)
    let executor = Executor(schema)
    let suaveConfig =
        { defaultConfig with
            bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" config.Port ]}
    let api = setCorsHeaders >=> graphql executor >=> Writers.setMimeType "application/json"
    startWebServer suaveConfig api
    membershipChannel.ShutdownAsync().Wait()
    0