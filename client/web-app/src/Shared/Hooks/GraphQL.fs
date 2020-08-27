namespace GraphQL

open Auth0
open Fable.SimpleHttp
open Feliz
open PublicApi
open PrivateApi

type GraphQLProviderValue =
    { publicApi: PublicApiGraphqlClient
      privateApi: PrivateApiGraphqlClient }

module private GraphQL =
    let defaultProviderValue =
        { publicApi = PublicApiGraphqlClient("http://localhost:4000/public")
          privateApi = PrivateApiGraphqlClient("http://localhost:4000") }

    let GraphQLContext = React.createContext("GraphQLContext", defaultProviderValue)

    type GraphQLProviderProps =
        { publicUrl: string
          privateUrl: string
          children: seq<ReactElement> }

    let provider =
        React.functionComponent<GraphQLProviderProps>(fun props ->
            let (token, setToken) = React.useState("")
            let bearer = sprintf "Bearer %s" token
            let auth0 = React.useAuth0()

            let getToken () =
                async {
                    let! token = auth0.getToken()
                    setToken token
                }

            React.useEffectOnce(getToken >> Async.StartImmediate)
            let providerValue =
                { publicApi = PublicApiGraphqlClient(props.publicUrl)
                  privateApi = PrivateApiGraphqlClient(props.privateUrl, [Header ("Authorization", bearer)]) }
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
