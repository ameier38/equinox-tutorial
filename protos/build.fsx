#r "paket:
github googleapis/googleapis
nuget BlackFox.Fake.BuildTask
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"
open BlackFox.Fake
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators

let prototoolImage = "prototool:latest"

let run (command:string) (args:string list) (workDir:string) =
    CreateProcess.fromRawCommand command args
    |> CreateProcess.withWorkingDirectory workDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let createMountArg (source:string) (target:string) =
    sprintf "--mount=type=bind,source=%s,target=%s" source target

let prototool (args:string list) =
    let googleApisDir = __SOURCE_DIRECTORY__ </> ".fake" </> "build.fsx" </> "paket-files" </> "googleapis"
    let googleApisMountArg = createMountArg googleApisDir "/vendor/googleapis"
    let workMountArg = createMountArg __SOURCE_DIRECTORY__ "/work"
    let dockerArgs =
        [ "run"
          googleApisMountArg
          workMountArg
          prototoolImage ]
    let prototoolArgs =
        [ yield! args
          yield "protos" ]
    __SOURCE_DIRECTORY__
    |> run "docker" (dockerArgs @ prototoolArgs) 

let buildPrototool = BuildTask.create "BuildPrototool" [] {
    __SOURCE_DIRECTORY__ // protos
    |> Path.getDirectory // equinox-tutorial
    </> ".github" </> "actions" </> "prototool"
    |> run "docker" ["build"; "-t"; prototoolImage; "."]
}

let clean = BuildTask.create "Clean" [] {
    Shell.cleanDir "gen"
}

let format = BuildTask.create "Format" [buildPrototool] {
    prototool ["format"; "-w"]
}    

let lint = BuildTask.create "Lint" [buildPrototool; format] {
    prototool ["lint"]
}

let compile = BuildTask.create "Compile" [buildPrototool; lint] {
    prototool ["compile"]
}

BuildTask.create "Generate" [buildPrototool; clean; compile] {
    prototool ["generate"; "--debug"]
}

BuildTask.createEmpty "Test" [buildPrototool; compile]

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
