module PrivateApp.Sidebar

open Auth0
open Elmish
open Feliz
open Feliz.MaterialUI
open Feliz.UseElmish

type NavItem =
    | Profile
    | Dashboard
    | Shop

type State =
    { CurrentUrl: string list
      NavItems: NavItem list
      SelectedNavItem: NavItem option }

type Msg =
    | UrlChanged of string list

let selectedNavItem =
    function
    | [] -> Some Dashboard
    | [ "profile" ] -> Some Profile
    | [ "shop" ] -> Some Shop
    | _ -> None

let init (currentUrl: string list): State * Cmd<Msg> =
    let state =
        { CurrentUrl = currentUrl
          NavItems = [ Dashboard; Profile; Shop ]
          SelectedNavItem = selectedNavItem currentUrl }
    state, Cmd.none

let update (msg:Msg) (state:State): State * Cmd<Msg> =
    match msg with
    | UrlChanged url ->
        let newState =
            { state with
                CurrentUrl = url
                SelectedNavItem = selectedNavItem url }
        newState, Cmd.none

let useStyles = Styles.makeStyles(fun styles _ ->
    {|
        profileItem = styles.create [
            style.height 250
        ]
        navItem = styles.create [
            style.height 100
        ]
    |}
)

type SidebarProps =
    { currentUrl: string list
      selectedNavItem: NavItem }

let render =
    React.functionComponent<SidebarProps>("Sidebar", fun props ->
        let init () = init props.currentUrl
        let state, dispatch = React.useElmish(init, update, [||])
        let { userProfile = profileOpt } = React.useAuth0()
        let classes = useStyles()
        Mui.drawer [
            Html.div [
                prop.className classes.profileItem
                prop.children [
                    match profileOpt with
                    | Some profile ->
                        Mui.avatar [
                            avatar.src profile.picture
                        ]
                    | None ->
                        Mui.avatar [
                            Icon.person
                        ]
                ]
            ]
            Mui.list [
                for navItem in state.NavItems do
                    Mui.listItem [
                        prop.className classes.navItem
                        prop.onClick (fun _ ->
                            let path =
                                match navItem with
                                | Dashboard -> []
                                | Profile -> ["profile"]
                                | Shop -> ["shop"]
                            dispatch (UrlChanged path))
                        listItem.selected (state.SelectedNavItem = Some navItem)
                        listItem.children [
                            Mui.listItemIcon [
                                match navItem with
                                | Dashboard -> Icon.dashboard
                                | Profile -> Icon.person
                                | Shop -> Icon.rocket
                            ]
                            Mui.listItemText [
                                match navItem with
                                | Dashboard -> "Dashboard"
                                | Profile -> "Profile"
                                | Shop -> "Shop"
                                |> listItemText.primary
                            ]
                        ]
                    ]
                    Mui.divider []
            ]
        ]
    )