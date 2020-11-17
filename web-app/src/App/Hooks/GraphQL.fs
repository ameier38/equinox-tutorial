namespace GraphQL

open Auth0
open Fable.SimpleHttp
open Feliz
open FSharp.UMX
open PublicClient
open PrivateClient

type GraphQLProviderValue =
    { publicClient: PublicGraphqlClient
      createPrivateClient: AccessToken -> PrivateGraphqlClient }

module private GraphQL =

    let defaultGraphQLProviderValue =
        { publicClient = PublicGraphqlClient("http://localhost:4000/public")
          createPrivateClient = fun _ -> PrivateGraphqlClient("http://localhost:4000") }

    let GraphQLContext = React.createContext("GraphQLContext", defaultGraphQLProviderValue)

    type GraphQLProviderProps =
        { publicUrl: string
          privateUrl: string
          children: seq<ReactElement> }

    let provider =
        React.functionComponent<GraphQLProviderProps>(fun props ->
            let providerValue =
                { publicClient = PublicGraphqlClient(props.publicUrl)
                  createPrivateClient = fun token ->
                    let bearer = sprintf "Bearer %s" (UMX.untag token)
                    PrivateGraphqlClient(props.privateUrl, [Header ("Authorization", bearer)]) }

            React.contextProvider(GraphQLContext, providerValue, props.children)
        )

type GraphQLProperty =
    | PublicUrl of string
    | PrivateUrl of string
    | Children of ReactElement list

type GraphQL =
    static member publicUrl = PublicUrl
    static member privateUrl = PrivateUrl
    static member children = Children
    static member provider (props:GraphQLProperty list) : ReactElement =
        let defaultProps: GraphQL.GraphQLProviderProps =
            { publicUrl = "http://localhost:4000/public"
              privateUrl = "http://localhost:4000"
              children = Seq.empty }
        let modifiedProps =
            (defaultProps, props)
            ||> List.fold (fun state prop ->
                match prop with
                | PublicUrl url -> { state with publicUrl = url }
                | PrivateUrl url -> { state with privateUrl = url }
                | Children children -> { state with children = children })
        GraphQL.provider modifiedProps

module React =
    let useGQL () =  React.useContext(GraphQL.GraphQLContext)
