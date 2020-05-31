module Auth0

open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz

type UserProfile =
    { email: string
      name: string
      user_id: string
      ``https://cosmicdealership.com/roles``: string list }

type Auth0ProviderValue =
    { isLoading: bool
      isAuthenticated: bool
      userProfile: UserProfile option
      getToken: unit -> Async<string>
      login: unit -> unit
      logout: unit -> unit }

[<RequireQualifiedAccess>]
module private Auth0Interop =
    type Auth0ClientOptions =
        { domain: string
          client_id: string
          redirect_uri: string
          audience: string }

    type IAuth0Client =
        abstract member loginWithRedirect: unit -> unit
        abstract member handleRedirectCallback: unit -> JS.Promise<unit>
        abstract member isAuthenticated: unit -> JS.Promise<bool>
        abstract member getUser: unit -> JS.Promise<UserProfile>
        abstract member getTokenSilently: unit -> JS.Promise<string>
        abstract member logout: unit -> unit

    type Auth0ProviderProps =
        { children: seq<ReactElement>
          domain: string
          clientId: string
          redirectUri: string
          audience: string }

    let createAuth0Client (opts:Auth0ClientOptions): JS.Promise<IAuth0Client> = importDefault "@auth0/auth0-spa-js"

    let defaultOnRedirectCallback () = history.replaceState((), document.title, window.location.pathname)

    let defaultAuth0Context =
        { isLoading = true
          isAuthenticated = false
          userProfile = None
          getToken = fun () -> async{ return "" }
          login = id<unit>
          logout = id<unit> }

    let Auth0Context = React.createContext("Auth0Context", defaultAuth0Context)

    let provider =
        React.functionComponent<Auth0ProviderProps>("Auth0Provider", fun props ->
            let (isLoading, setIsLoading) = React.useState(true)
            let (isAuthenticated, setIsAuthenticated) = React.useState(false)
            let (userProfile, setUserProfile) = React.useState<UserProfile option>(None)
            let (auth0Client, setAuth0Client) = React.useState<IAuth0Client option>(None)

            let initAuth0Client () =
                async {
                    let clientOpts =
                        { domain = props.domain
                          client_id = props.clientId
                          redirect_uri = props.redirectUri
                          audience = props.audience }
                    let! auth0Client = createAuth0Client clientOpts |> Async.AwaitPromise
                    setAuth0Client (Some auth0Client)
                    if window.location.search.Contains("code=") then
                        do! auth0Client.handleRedirectCallback() |> Async.AwaitPromise
                        defaultOnRedirectCallback()
                    let! isAuthenticated = auth0Client.isAuthenticated() |> Async.AwaitPromise
                    setIsAuthenticated(isAuthenticated)
                    if isAuthenticated then
                        let! userProfile = auth0Client.getUser() |> Async.AwaitPromise
                        setUserProfile(Some userProfile)
                    setIsLoading(false)
                }
            
            let getToken () =
                match auth0Client with
                | Some client -> client.getTokenSilently() |> Async.AwaitPromise
                | None -> async { return "" }

            let login () =
                match auth0Client with
                | Some client -> client.loginWithRedirect()
                | None -> ()

            let logout () =
                match auth0Client with
                | Some client -> client.logout()
                | None -> ()

            React.useEffect(initAuth0Client >> Async.StartImmediate, [| |])
            let providerValue =
                { isLoading = isLoading
                  isAuthenticated = isAuthenticated
                  userProfile = userProfile
                  getToken = getToken
                  login = login
                  logout = logout }
            React.contextProvider(Auth0Context, providerValue, props.children)
        )

type Auth0Property =
    | Domain of string
    | ClientId of string
    | RedirectUri of string
    | Audience of string
    | Children of ReactElement list

type Auth0 =
    static member domain = Domain
    static member clientId = ClientId
    static member redirectUri = RedirectUri
    static member audience = Audience
    static member children = Children
    static member provider (props:Auth0Property list) : ReactElement =
        let defaultProps: Auth0Interop.Auth0ProviderProps =
            { children = Seq.empty
              domain = ""
              clientId = ""
              redirectUri = ""
              audience = "" }
        let modifiedProps =
            (defaultProps, props)
            ||> List.fold (fun state prop ->
                match prop with
                | Domain domain -> { state with domain = domain }
                | ClientId clientId -> { state with clientId = clientId }
                | RedirectUri redirectUri -> { state with redirectUri = redirectUri }
                | Audience audience -> { state with audience = audience }
                | Children children -> { state with children = children })
        Auth0Interop.provider modifiedProps
        
module Hooks =
    let useAuth0 () = React.useContext(Auth0Interop.Auth0Context)
