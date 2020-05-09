module Auth0

open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz

type Auth0ClientOptions =
    { domain: string
      client_id: string
      redirect_uri: string }

type UserProfile =
    { email: string
      name: string
      user_id: string }

type IAuth0Client =
    abstract member loginWithRedirect: unit -> unit
    abstract member handleRedirectCallback: unit -> JS.Promise<unit>
    abstract member isAuthenticated: unit -> JS.Promise<bool>
    abstract member getUser: unit -> JS.Promise<UserProfile>
    abstract member logout: unit -> unit

type Auth0ProviderValue =
    { isLoading: bool
      isAuthenticated: bool
      userProfile: UserProfile option
      login: unit -> unit
      logout: unit -> unit }

let createAuth0Client (opts:Auth0ClientOptions): JS.Promise<IAuth0Client> = importDefault "@auth0/auth0-spa-js"

let defaultOnRedirectCallback () = history.replaceState((), document.title, window.location.pathname)

let defaultAuth0Context =
    { isLoading = true
      isAuthenticated = false
      userProfile = None
      login = id<unit>
      logout = id<unit> }

let Auth0Context = React.createContext("Auth0Context", defaultAuth0Context)

let useAuth0 () = React.useContext(Auth0Context)

let auth0Provider =
    React.functionComponent("Auth0Provider", fun (props:{| children: seq<ReactElement> |}) ->
        let (isLoading, setIsLoading) = React.useState(true)
        let (isAuthenticated, setIsAuthenticated) = React.useState(false)
        let (userProfile, setUserProfile) = React.useState<UserProfile option>(None)
        let (auth0Client, setAuth0Client) = React.useState<IAuth0Client option>(None)

        let initAuth0Client () =
            async {
                let clientOpts =
                    { domain = Config.auth0Config.Domain
                      client_id = Config.auth0Config.ClientId
                      redirect_uri = Config.appConfig.Url }
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

        let handleRedirectCallback () =
            async {
                setIsLoading(true)
                match auth0Client with
                | Some client ->
                    do! client.handleRedirectCallback() |> Async.AwaitPromise
                    let! userProfile = client.getUser() |> Async.AwaitPromise
                    setUserProfile(Some userProfile)
                    setIsAuthenticated(true)
                    setIsLoading(false)
                | None ->
                    failwithf "auth0 client not initialized"
            }
        
        React.useEffect(initAuth0Client >> Async.StartImmediate, [| |])
        let providerValue =
            { isLoading = isLoading
              isAuthenticated = isAuthenticated
              userProfile = userProfile
              login = match auth0Client with Some client -> client.loginWithRedirect | _ -> id<unit>
              logout = match auth0Client with Some client -> client.logout | _ -> id<unit> }
        React.contextProvider(Auth0Context, providerValue, props.children)
    )

let provider (children:seq<ReactElement>) = auth0Provider {| children = children |}
