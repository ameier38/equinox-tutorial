module GraphQL

open Auth0
open Fable
open Fable.Core
open Fable.Core.JsInterop
open Feliz

// force use of input object
// ref: https://blog.apollographql.com/designing-graphql-mutations-e09de826ed97
type Variables<'I> = { input: 'I }
        
type UseQueryOptions<'I> =
    { variables: Variables<'I> }

type UseQueryResponse<'D> =
    { loading: bool
      error: obj option
      data: 'D }

[<RequireQualifiedAccess>]
module private GraphQLInterop =
    type GraphQLClientOptions =
        { url: string }

    type IGraphQLClient =
        abstract member setHeader: string * string -> unit

    type IGraphQLClientStatic =
        [<Emit("new $0($1)")>]
        abstract Create: GraphQLClientOptions -> IGraphQLClient

    let ClientContext: React.IContext<IGraphQLClient>  = importMember "graphql-hooks"

    let GraphQLClient: IGraphQLClientStatic = importMember "graphql-hooks"

    type GraphQLProviderProps =
        { client: IGraphQLClient
          children: seq<ReactElement> }

    let provider =
        React.functionComponent<GraphQLProviderProps>("GraphQLProvider", fun props ->
            let auth0 = Hooks.useAuth0()
            let initGqlClient () =
                async {
                    let! token = auth0.getToken()
                    Log.debug ("got token", token)
                    Log.debug "setting auth header"
                    props.client.setHeader("Authorization", sprintf "Bearer %s" token)
                    Log.debug "setting client"
                }
            React.useEffectOnce(initGqlClient >> Async.StartImmediate)
            React.contextProvider(ClientContext, props.client, props.children)
        )

    let useQuery<'V,'D>(query:string, options:UseQueryOptions<'V>): UseQueryResponse<'D> = importMember "graphql-hooks"

type GraphQLProperty =
    | Url of string
    | Children of ReactElement list

type GraphQL =
    static member url = Url
    static member children = Children
    static member provider (props:GraphQLProperty list): ReactElement =
        let emptyClient = GraphQLInterop.GraphQLClient.Create({ url = "http://localhost:4000"})
        let defaultProps: GraphQLInterop.GraphQLProviderProps =
            { client = emptyClient
              children = List.empty }
        let modifiedProps =
            (defaultProps, props)
            ||> List.fold (fun state prop ->
                match prop with
                | Url url -> { state with client = GraphQLInterop.GraphQLClient.Create({ url = url})}
                | Children children -> { state with children = children }
            )
        GraphQLInterop.provider modifiedProps

module Hooks =
    let useQuery<'I,'D>(query:string, input:'I): UseQueryResponse<'D> =
        GraphQLInterop.useQuery(query, { variables = { input = input }})
