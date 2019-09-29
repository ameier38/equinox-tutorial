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
open Fake.Core.TargetOperators

let prototoolImage = "uber/prototool:1.8.1"

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
        [ yield "prototool"
          yield! args
          yield "protos" ]
    __SOURCE_DIRECTORY__
    |> run "docker" (dockerArgs @ prototoolArgs) 

let clean = BuildTask.create "Clean" [] {
    Shell.cleanDir "gen"
}

let format = BuildTask.create "Format" [] {
    prototool ["format"; "-w"]
}    

let lint = BuildTask.create "Lint" [format] {
    prototool ["lint"]
}

let compile = BuildTask.create "Compile" [lint] {
    prototool ["compile"]
}

BuildTask.create "Generate" [clean; compile] {
    prototool ["generate"; "--debug"]
}

BuildTask.createEmpty "Test" [compile]

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
