namespace GraphQL

open Auth0
open Fable.SimpleHttp
open Feliz
open Snowflaqe

type GraphQLProviderValue =
    { client: SnowflaqeGraphqlClient }

module private GraphQL =
    let defaultProviderValue =
        { client = SnowflaqeGraphqlClient("http://localhost:4000") }

    let GraphQLContext = React.createContext("GraphQLContext", defaultProviderValue)

    type GraphQLProviderProps =
        { scheme: string
          host: string
          port: int
          children: seq<ReactElement> }

    let provider =
        React.functionComponent<GraphQLProviderProps>(fun props ->
            let url = sprintf "%s://%s:%i" props.scheme props.host props.port
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
                { client = SnowflaqeGraphqlClient(url, [Header ("Authorization", bearer)]) }
            React.contextProvider(GraphQLContext, providerValue, props.children)
        )

type GraphQLProperty =
    | Scheme of string
    | Host of string
    | Port of int
    | Children of seq<ReactElement>

type GraphQL =
    static member scheme = Scheme
    static member host = Host
    static member port = Port
    static member children = Children
    static member provider (props:GraphQLProperty list) : ReactElement =
        let defaultProps: GraphQL.GraphQLProviderProps =
            { scheme = "http"
              host = "localhost"
              port = 4000
              children = Seq.empty }
        let modifiedProps =
            (defaultProps, props)
            ||> List.fold (fun state prop ->
                match prop with
                | Scheme scheme -> { state with scheme = scheme }
                | Host host -> { state with host = host }
                | Port port -> { state with port = port }
                | Children children -> { state with children = children })
        GraphQL.provider modifiedProps

module React =
    let useGQL () =  React.useContext(GraphQL.GraphQLContext)
