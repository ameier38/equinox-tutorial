module Page.Vehicles

open Elmish
open Feliz
open Feliz.MaterialUI
open Feliz.UseDeferred
open GraphQL
open Auth0
open PrivateClient

let render =
    React.functionComponent<unit>(fun _ ->
        let auth0 = React.useAuth0()
        let { createPrivateClient = createPrivateClient } = React.useGQL()
        let input: ListVehicles.InputVariables = { input = { pageToken = None; pageSize = Some 10 } }

        let loadVehicles =
            async {
                let! token = auth0.getToken()
                let privateClient = createPrivateClient token
                return! privateClient.ListVehicles(input)
            }

        let data = React.useDeferred(loadVehicles, [||])

        Common.PageContainer.render true [
            Mui.typography [
                typography.variant.h2
                prop.text "Vehicles"
            ]
            match data with
            | Deferred.HasNotStartedYet
            | Deferred.InProgress ->
                let cards = [ for _ in 0..5 -> Common.Grid.skeletonCard() ]
                Common.Grid.gridContainer ({| cards = cards |})
            | Deferred.Failed err ->
                Html.h1 (sprintf "Error!: %A" err)
            | Deferred.Resolved (Error errors) ->
                Html.h1 (sprintf "Error!: %A" errors)
            | Deferred.Resolved (Ok { listVehicles = res }) ->
                match res with
                | ListVehicles.ListVehiclesResponse.ListVehiclesSuccess success ->
                    let cards =
                        [ for vehicle in success.vehicles ->
                            Common.Grid.vehicleCard
                                ({ vehicleId = vehicle.vehicleId
                                   make = vehicle.vehicle.make
                                   model = vehicle.vehicle.model
                                   status = vehicle.status.ToString() })
                        ]
                    Common.Grid.gridContainer ({| cards = cards |})
                | other ->
                    Html.h1 (sprintf "Error!: %A" other)
        ]
    )
