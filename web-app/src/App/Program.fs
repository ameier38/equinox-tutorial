module Program

open Elmish
open Elmish.React

// NB: set in fable-loader options in webpack config
#if DEVELOPMENT
open Elmish.HMR
#endif

Program.mkProgram App.init App.update App.render 
|> Program.withReactSynchronous "app"
|> Program.run
