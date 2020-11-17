module Common.PageContainer

open Feliz
open Feliz.MaterialUI

let useStyles = Styles.makeStyles(fun styles _ ->
    {|
        padding = styles.create (fun padNav -> [
            if padNav then
                style.paddingTop 100
        ])
    |}
)

let render' =
    React.functionComponent(fun (props: {| padNav: bool; children: ReactElement list |}) ->
        let c = useStyles(props.padNav)
        let theme = Styles.useTheme()
        let isGteMd = Hooks.useMediaQuery(theme.breakpoints.upMd)
        Mui.container [
            prop.className c.padding
            container.disableGutters (not isGteMd)
            container.maxWidth.md
            container.children props.children
        ]
    )

let render padNav children = render' {| padNav = padNav; children = children |}
