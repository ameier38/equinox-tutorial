namespace Auth0

open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Feliz
open FSharp.UMX

type Role =
    | Admin
    | Customer
module Role =
    let fromDto (roles:string array) =
        if roles |> Array.contains "admin" then Admin
        else Customer

type ProfileDto =
    { email: string
      name: string
      sub: string
      picture: string
      ``https://cosmicdealership.com/roles``: string array }

type Profile =
    { UserId: UserId
      UserName: UserName
      Email: Email
      Avatar: Avatar
      Role: Role }
module Profile =
    let fromDto (dto:ProfileDto) =
        { UserId = UMX.tag<userId> dto.sub
          UserName = UMX.tag<userName> dto.name
          Email = UMX.tag<email> dto.email
          Avatar = UMX.tag<avatar> dto.picture
          Role = Role.fromDto dto.``https://cosmicdealership.com/roles``  }

type User =
    | Unauthenticated
    | Authenticated of Profile

type Auth0ProviderValue =
    { isLoading: bool
      isAuthenticated: bool
      user: User
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
        abstract member getUser: unit -> JS.Promise<ProfileDto>
        abstract member getTokenSilently: unit -> JS.Promise<string>
        abstract member logout: unit -> unit

    type IAuth0ClientStatic =
        [<Emit("new $0($1)")>]
        abstract member create: opts:Auth0ClientOptions -> IAuth0Client

    type Auth0ProviderProps =
        { children: seq<ReactElement>
          domain: string
          clientId: string
          redirectUri: string
          audience: string }

    let Auth0Client: IAuth0ClientStatic = importMember "@auth0/auth0-spa-js"

    let defaultOnRedirectCallback () = history.replaceState((), document.title, window.location.pathname)

    let defaultAuth0Context =
        { isLoading = true
          isAuthenticated = false
          user = Unauthenticated
          getToken = fun () -> failwithf "Auth0 not initialized"
          login = id<unit>
          logout = id<unit> }

    let Auth0Context = React.createContext("Auth0Context", defaultAuth0Context)

    let provider =
        React.functionComponent<Auth0ProviderProps>("Auth0Provider", fun props ->
            let (isLoading, setIsLoading) = React.useState(true)
            let (user, setUser) = React.useState<User>(Unauthenticated)
            let opts: Auth0ClientOptions =
                { domain = props.domain
                  client_id = props.clientId
                  redirect_uri = props.redirectUri
                  audience = props.audience }
            let auth0Client = Auth0Client.create(opts)

            let init () =
                Log.debug "initializing auth0..."
                promise {
                    try
                        if window.location.search.Contains("code=") then
                            do! auth0Client.handleRedirectCallback()
                            defaultOnRedirectCallback()
                        let! isAuthenticated = auth0Client.isAuthenticated()
                        if isAuthenticated then
                            let! profileDto = auth0Client.getUser()
                            let profile = Profile.fromDto profileDto
                            Log.debug (sprintf "profile: %A" profile)
                            setUser(Authenticated profile)
                        else
                            setUser(Unauthenticated)
                        setIsLoading(false)
                    with ex ->
                        Log.error(ex)
                        setUser(Unauthenticated)
                        setIsLoading(false)
                }
            
            React.useEffectOnce(init >> Promise.start)

            let getToken () = auth0Client.getTokenSilently() |> Async.AwaitPromise

            let login () = auth0Client.loginWithRedirect()

            let logout () = auth0Client.logout()

            let providerValue =
                { isLoading = isLoading
                  isAuthenticated = match user with Authenticated _ -> true | Unauthenticated -> false
                  user = user
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
        
module React =
    let useAuth0 () = React.useContext(Auth0Interop.Auth0Context)
