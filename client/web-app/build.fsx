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

let cleanPublicClient = BuildTask.create "CleanPublicClient" [] {
    Shell.cleanDir "src/PublicClient"
}

let generatePublicClient = BuildTask.create "GeneratePublicClient" [cleanPublicClient] {
    // reads from `snowflaqePublic.json` and generates the public api client projet
    snowflaqe ["--config"; "snowflaqePublic.json"; "--generate"]
}

let cleanPrivateClient = BuildTask.create "CleanPrivateApi" [] {
    Shell.cleanDir "src/PrivateApi"
}

let generatePrivateClient = BuildTask.create "GeneratePrivateApi" [cleanPrivateClient] {
    // reads from `snowflaqePrivate.json` and generates the private api client projet
    snowflaqe ["--config"; "snowflaqePrivate.json"; "--generate"]
}

let generate = BuildTask.createEmpty "GenerateClients" [generatePublicClient; generatePrivateClient]

BuildTask.create "Restore" [clean] {
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
}

BuildTask.create "RestoreTests" [clean.IfNeeded] {
    !! "test/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
}

BuildTask.create "Install" [] {
    Npm.install id
}

BuildTask.create "StartCustomerApp" [] {
    Npm.run "startCustomerApp" id
}

BuildTask.create "StartAdminApp" [] {
    Npm.run "startAdminApp" id
}

BuildTask.create "BuildCustomerApp" [clean; generate] {
    Npm.run "buildCustomerApp" id
}

BuildTask.create "BuildAdminApp" [clean; generate] {
    Npm.run "buildAdminApp" id
}

BuildTask.create "TestIntegrations" [] {
    let result = DotNet.exec id "run" "--project test/IntegrationTests/IntegrationTests.fsproj"
    if not result.OK then failwithf "Error! %A" result.Errors
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
