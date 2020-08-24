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

let cleanPublicApi = BuildTask.create "CleanPublicApi" [] {
    Shell.cleanDir "src/PublicApi"
}

let generatePublicApi = BuildTask.create "GeneratePublicApi" [cleanPublicApi] {
    // reads from `publicApi.json` and generates the public api client projet
    snowflaqe ["--config"; "publicApi.json"; "--generate"]
}

let cleanPrivateApi = BuildTask.create "CleanPrivateApi" [] {
    Shell.cleanDir "src/PrivateApi"
}

let generatePrivateApi = BuildTask.create "GeneratePrivateApi" [cleanPrivateApi] {
    // reads from `privateApi.json` and generates the private api client projet
    snowflaqe ["--config"; "privateApi.json"; "--generate"]
}

let generate = BuildTask.createEmpty "Generate" [generatePublicApi; generatePrivateApi]

BuildTask.create "Restore" [clean.IfNeeded] {
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
}

BuildTask.create "Install" [] {
    Npm.install id
}

BuildTask.create "Serve" [] {
    Npm.run "start" id
}

BuildTask.create "Build" [clean; generate] {
    Npm.run "build" id
}

BuildTask.create "TestIntegrations" [] {
    let result = DotNet.exec id "run" "src/IntegrationTests/IntegrationTests.fsproj"
    if not result.OK then failwithf "Error! %A" result.Errors
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
