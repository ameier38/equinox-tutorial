module Page.Vehicle

open Elmish
open Feliz
open Feliz.MaterialUI
open Feliz.UseDeferred
open FSharp.UMX
open GraphQL
open PublicClient

let useStyles = Styles.makeStyles(fun styles _ ->
    {|
        media = styles.create [
            style.height 140
        ]
    |}
)

type VehicleProps =
    { vehicleId: VehicleId }

let render =
    React.functionComponent<VehicleProps>(fun props ->
        let c = useStyles()
        let { publicClient = gql } = React.useGQL()
        let input: GetAvailableVehicle.InputVariables =
            { input =
                { vehicleId = UMX.untag props.vehicleId } }
        let data = React.useDeferred(gql.GetAvailableVehicle(input), [||])
        Common.PageContainer.render true [
            Mui.card [
                card.children [
                    match data with
                    | Deferred.HasNotStartedYet
                    | Deferred.InProgress ->
                        Mui.card [
                            card.children [
                                Mui.skeleton [
                                    prop.className c.media
                                    skeleton.variant.rect
                                ]
                                Mui.cardContent [
                                    Mui.skeleton [
                                        skeleton.animation.wave
                                        skeleton.width (length.perc 100)
                                    ]
                                ]
                            ]
                        ]
                    | Deferred.Failed err ->
                        Log.debug err
                        Mui.card [
                            Error.renderError (err.ToString())
                        ]
                    | Deferred.Resolved (Error errors) ->
                        Log.debug errors
                        Mui.card [
                            Error.renderError (errors |> List.map (fun e -> e.message) |> String.concat "; ")
                        ]
                    | Deferred.Resolved (Ok { getAvailableVehicle = res }) ->
                        match res with
                        | GetAvailableVehicle.GetAvailableVehicleResponse.VehicleNotFound { message = msg } ->
                            Log.debug msg
                            Mui.card [
                                Error.renderError msg
                            ]
                        | GetAvailableVehicle.GetAvailableVehicleResponse.InventoriedVehicle inventoriedVehicle ->
                            let makeModel = sprintf "%s %s" inventoriedVehicle.vehicle.make inventoriedVehicle.vehicle.model
                            Mui.card [
                                card.children [
                                    Mui.cardContent [
                                        Mui.typography [
                                            prop.id "title"
                                            prop.text makeModel
                                            typography.variant.h6
                                        ]
                                        Mui.typography [
                                            typography.variant.body2
                                            prop.text (sprintf "This vehicle is %A" inventoriedVehicle.status)
                                        ]
                                    ]
                                ]
                            ]
                ]
            ]
        ]
    )
