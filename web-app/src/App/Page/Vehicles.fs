module Page.Vehicles

open Feliz
open Feliz.MaterialUI
open Feliz.UseDeferred
open GraphQL
open Auth0
open PrivateClient

let useStyles =
    Styles.makeStyles (fun styles _ -> {| root = styles.create [ style.paddingTop 100 ] |})

let render =
    React.functionComponent<unit> (fun _ ->
        let c = useStyles ()
        let auth0 = React.useAuth0()
        let { privateClient = gql } = React.useGQL ()
        let isLoading, setIsLoading = React.useState(true)
        let error, setError = React.useState<string option>(None)
        let prevPageToken, setPrevPageToken = React.useState<string option>(None)
        let nextPageToken, setNextPageToken = React.useState<string option>(None)
        let pageToken, setPageToken = React.useState<string option>(None)
        let currentPage, setCurrentPage = React.useState (1)
        let pageSize, setPageSize = React.useState (10)
        let totalCount, setTotalCount = React.useState(0)
        let vehicles, setVehicles = React.useState<ListVehicles.InventoriedVehicle list>([])

        let listVehicles () =
            async {
                let input: ListVehicles.InputVariables =
                    { input =
                          { pageToken = pageToken
                            pageSize = Some pageSize } }
                match gql with
                | Some gql ->
                    let! res = gql.ListVehicles(input)
                    match res with
                    | Ok ({ listVehicles = ListVehicles.ListVehiclesResponse.ListVehiclesSuccess success}) ->
                        setError None
                        setIsLoading false
                        setTotalCount success.totalCount
                        setPrevPageToken pageToken
                        setNextPageToken (Some success.nextPageToken)
                        setVehicles success.vehicles
                    | Ok ({ listVehicles = ListVehicles.ListVehiclesResponse.PageSizeInvalid { message = msg }})
                    | Ok ({ listVehicles = ListVehicles.ListVehiclesResponse.PageTokenInvalid { message = msg }})
                    | Ok ({ listVehicles = ListVehicles.ListVehiclesResponse.PermissionDenied { message = msg }}) ->
                        setError (Some msg)
                        setIsLoading false
                        setVehicles []
                    | Error errors ->
                        let msg = errors |> List.map (fun e -> e.message) |> String.concat "; "
                        setError (Some msg)
                        setIsLoading false
                        setVehicles []
                | None ->
                    setIsLoading true
            }

        let data = React.useDeferred (listVehicles(), [|gql; pageSize|])

        Mui.container [
            prop.className c.root
            container.children [
                Mui.container [
                    prop.className c.root
                    container.children [ 
                        match isLoading, error, data with
                        | true, _, _ -> Html.p "loading..."
                        | false, Some error, _ -> Html.p error
                        | false, None, Deferred.HasNotStartedYet -> Html.p "has not started"
                        | false, None, Deferred.InProgress -> Html.p "in progress"
                        | false, None, Deferred.Failed _ -> Html.p "failed"
                        | false, None, Deferred.Resolved _ ->
                            Html.div [
                                for vehicle in vehicles ->
                                    Html.p vehicle.vehicleId
                            ]
                    ]
                ]
            ]
        ]

                // match data with
                // | Deferred.HasNotStartedYet
                // | Deferred.InProgress -> materialTable.isLoading true
                // | Deferred.Failed exn -> Log.error exn
                // | Deferred.Resolved (Error errors) -> Log.error errors
                // | Deferred.Resolved (Ok { listVehicles = res }) ->
                //     match res with
                //     | ListVehicles.ListVehiclesResponse.PageTokenInvalid { message = msg }
                //     | ListVehicles.ListVehiclesResponse.PageSizeInvalid { message = msg }
                //     | ListVehicles.ListVehiclesResponse.PermissionDenied { message = msg } ->
                //         Html.div [msg]
                //     | ListVehicles.ListVehiclesResponse.ListVehiclesSuccess data ->
                //         setPrevPageToken (Some res)
                //         setNextPageToken res.nextPageToken
                //         Html.div [ "success" ]
                        //   Mui.materialTable [
                        //     materialTable.title "Vehicles"
                        //     materialTable.options [
                        //         options.pageSize pageSize
                        //         options.showFirstLastPageButtons false
                        //     ]
                        //     materialTable.onChangeRowsPerPage setPageSize
                        //     materialTable.onChangePage (fun _ page ->
                        //         if page = 1 then setPageToken None
                        //         elif page < currentPage then setPageToken prevPageToken
                        //         elif page > currentPage then setPageToken nextPageToken
                        //         else setPageToken pageToken)
                        //     materialTable.columns [
                        //         columns.column [
                        //             column.title "ID"
                        //             column.field<ListVehicles.InventoriedVehicle> (fun v -> nameof v.vehicleId)
                        //         ]
                        //         columns.column [
                        //             column.title "Make"
                        //             column.field<ListVehicles.InventoriedVehicle> (fun v -> nameof v.vehicle.make)
                        //         ]
                        //         columns.column [
                        //             column.title "Model"
                        //             column.field<ListVehicles.InventoriedVehicle> (fun v -> nameof v.vehicle.model)
                        //         ]
                        //         columns.column [
                        //             column.title "Year"
                        //             column.field<ListVehicles.InventoriedVehicle> (fun v -> nameof v.vehicle.year)
                        //         ]
                        //     ]
                            
                        // materialTable.data res.vehicles
        )
