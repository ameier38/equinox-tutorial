#r "paket:
nuget BlackFox.Fake.BuildTask
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"
open BlackFox.Fake
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

let run (command:string) (args:string list) (workDir:string) =
    CreateProcess.fromRawCommand command args
    |> CreateProcess.withWorkingDirectory workDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let buf (args: string list) =
    __SOURCE_DIRECTORY__
    |> run "buf" args

let clean = BuildTask.create "Clean" [] {
    __SOURCE_DIRECTORY__ </> "gen"
    |> Shell.cleanDir
}

let lint = BuildTask.create "Lint" [] {
    buf ["check"; "lint"]
}

BuildTask.create "Generate" [ clean; lint ] {
    buf ["generate"; "--path"; "proto/cosmicdealership"]
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
