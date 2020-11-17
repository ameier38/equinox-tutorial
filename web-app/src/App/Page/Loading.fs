module Page.Loading

open Elmish
open Feliz
open Feliz.MaterialUI

let render =
    React.functionComponent<unit>(fun _ ->
        Common.PageContainer.render true [
            Mui.linearProgress [
                linearProgress.variant.indeterminate
            ]
        ]
    )
