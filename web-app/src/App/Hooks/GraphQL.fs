namespace GraphQL

open Auth0
open Fable.SimpleHttp
open Feliz
open PublicClient
open PrivateClient

type GraphQLProviderValue =
    { publicClient: PublicGraphqlClient
      privateClient: PrivateGraphqlClient option }

module private GraphQL =
    let defaultProviderValue =
        { publicClient = PublicGraphqlClient("http://localhost:4000/public")
          privateClient = None }

    let GraphQLContext = React.createContext("GraphQLContext", defaultProviderValue)

    type GraphQLProviderProps =
        { publicUrl: string
          privateUrl: string
          children: seq<ReactElement> }

    let provider =
        React.functionComponent<GraphQLProviderProps>(fun props ->
            let auth0 = React.useAuth0()
            let token, setToken = React.useState<string option>(None)
            let getToken() = async {
                let! token = auth0.getToken()
                Log.debug (sprintf "GraphQL.token: %A" token)
                setToken(Some token)
            }
            React.useEffect(getToken >> Async.StartImmediate, [|box auth0.isAuthenticated|])

            let providerValue =
                { publicClient = PublicGraphqlClient(props.publicUrl)
                  privateClient =
                    token
                    |> Option.map (fun token ->
                        let bearer = sprintf "Bearer %s" token
                        PrivateGraphqlClient(props.privateUrl, [Header ("Authorization", bearer)])) }

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
