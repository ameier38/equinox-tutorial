module PublicApp.Vehicle

open Feliz
open Feliz.MaterialUI

type VehicleProps =
    { vehicleId: VehicleId }

let render =
    React.functionComponent<VehicleProps>(fun props ->
        Mui.card [
            card.children [
                Mui.cardMedia
            ]
        ]
    )