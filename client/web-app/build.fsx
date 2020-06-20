#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.JavaScript
open BlackFox.Fake

let run (command:string) (args:string list) =
    CreateProcess.fromRawCommand command args
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let snowflaqe = run "snowflaqe"

let clean = BuildTask.create "Clean" [] {
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
}

let cleanSnowflaqe = BuildTask.create "CleanSnowflaqe" [] {
    Shell.cleanDir "src/Snowflaqe"
}

let generateSnowflaqe = BuildTask.create "GenerateSnowflaqe" [cleanSnowflaqe] {
    // reads from `snowflaqe.json` and generates GraphQL client project
    snowflaqe ["--generate"]
}

BuildTask.create "Restore" [clean.IfNeeded] {
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
}

let install = BuildTask.create "Install" [] {
    Npm.install id
}

let serve = BuildTask.create "Serve" [] {
    Npm.run "start" id
}

let build = BuildTask.create "Build" [generateSnowflaqe] {
    Npm.run "build" id
}

BuildTask.create "TestIntegrations" [] {
    let result = DotNet.exec id "run" "src/IntegrationTests/IntegrationTests.fsproj"
    if not result.OK then failwithf "Error! %A" result.Errors
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
