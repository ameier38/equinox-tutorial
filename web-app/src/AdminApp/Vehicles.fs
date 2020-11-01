module Vehicles

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.MaterialTable
open Feliz.UseDeferred
open GraphQL
open PrivateClient

let useStyles =
    Styles.makeStyles (fun styles _ -> {| root = styles.create [ style.paddingTop 100 ] |})

let render =
    React.functionComponent<unit> (fun _ ->
        let c = useStyles ()
        let prevPageToken, setPrevPageToken = React.useState<string option>(None)
        let nextPageToken, setNextPageToken = React.useState<string option>(None)
        let pageToken, setPageToken = React.useState<string option>(None)
        let currentPage, setCurrentPage = React.useState (1)
        let pageSize, setPageSize = React.useState (10)
        let totalCount, setTotalCount = React.useState(0)
        let vehicles, setVehicles = React.useState<ListVehicles.InventoriedVehicle list>([])
        let { privateClient = gql } = React.useGQL ()

        let input: ListVehicles.InputVariables =
            { input =
                  { pageToken = pageToken
                    pageSize = Some pageSize } }

        let data =
            React.useDeferred (gql.ListVehicles(input), [||])

        Mui.container [
            prop.className c.root
            container.children
                [ Mui.materialTable [
                    materialTable.title "Vehicles"
                    materialTable.options [
                        options.pageSize pageSize
                        options.showFirstLastPageButtons false
                    ]
                    materialTable.onChangeRowsPerPage setPageSize
                    materialTable.onChangePage (fun _ page ->
                        if page = 1 then setPageToken None
                        elif page < currentPage then setPageToken prevPageToken
                        elif page > currentPage then setPageToken nextPageToken
                        else setPageToken pageToken)
                    materialTable.columns [
                        columns.column [
                            column.title "ID"
                            column.field<ListVehicles.InventoriedVehicle> (fun v -> nameof v.vehicleId)
                        ]
                        columns.column [
                            column.title "Make"
                            column.field<ListVehicles.InventoriedVehicle> (fun v -> nameof v.vehicle.make)
                        ]
                        columns.column [
                            column.title "Model"
                            column.field<ListVehicles.InventoriedVehicle> (fun v -> nameof v.vehicle.model)
                        ]
                        columns.column [
                            column.title "Year"
                            column.field<ListVehicles.InventoriedVehicle> (fun v -> nameof v.vehicle.year)
                        ]
                    ]
                    match data with
                    | Deferred.HasNotStartedYet
                    | Deferred.InProgress -> materialTable.isLoading true
                    | Deferred.Failed exn -> Log.error exn
                    | Deferred.Resolved (Error errors) -> Log.error errors
                    | Deferred.Resolved (Ok { listVehicles = res }) ->
                        setPrevPageToken (Some res)
                        setNextPageToken res.nextPageToken
                        materialTable.data res.vehicles
                  ] ]
        ])
