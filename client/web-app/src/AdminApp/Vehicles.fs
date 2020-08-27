module Vehicles

open Feliz
open Feliz.MaterialUI
open Feliz.MaterialUI.MaterialTable
open Feliz.UseDeferred
open GraphQL
open PrivateApi

let useStyles =
    Styles.makeStyles (fun styles _ -> {| root = styles.create [ style.paddingTop 100 ] |})

let render =
    React.functionComponent<unit> (fun _ ->
        let c = useStyles ()
        let prevPageToken, setPrevPageToken = React.useState ("")
        let nextPageToken, setNextPageToken = React.useState ("")
        let pageToken, setPageToken = React.useState ("")
        let currentPage, setCurrentPage = React.useState (1)
        let pageSize, setPageSize = React.useState (10)
        let { privateApi = gql } = React.useGQL ()

        let input: ListVehicles.InputVariables =
            { input =
                  { pageToken = Some pageToken
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
                        if page = 1 then setPageToken ""
                        elif page < currentPage then setPageToken prevPageToken
                        elif page > currentPage then setPageToken nextPageToken
                        else setPageToken pageToken)
                    materialTable.columns [
                        columns.column [
                            column.title "ID"
                            column.field<ListVehicles.VehicleState> (fun v -> nameof v.vehicleId)
                        ]
                        columns.column [
                            column.title "Make"
                            column.field<ListVehicles.VehicleState> (fun v -> nameof v.make)
                        ]
                        columns.column [
                            column.title "Model"
                            column.field<ListVehicles.VehicleState> (fun v -> nameof v.model)
                        ]
                        columns.column [
                            column.title "Year"
                            column.field<ListVehicles.VehicleState> (fun v -> nameof v.year)
                        ]
                    ]
                    match data with
                    | Deferred.HasNotStartedYet
                    | Deferred.InProgress -> materialTable.isLoading true
                    | Deferred.Failed exn -> Log.error exn
                    | Deferred.Resolved (Error errors) -> Log.error errors
                    | Deferred.Resolved (Ok { listVehicles = res }) ->
                        setPrevPageToken res.prevPageToken
                        setNextPageToken res.nextPageToken
                        materialTable.data res.vehicles
                  ] ]
        ])
