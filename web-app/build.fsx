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
    ++ "src/**/out"
    |> Shell.cleanDirs 
    !! "**/*.fs.js"
    |> File.deleteAll
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

BuildTask.createEmpty "GenerateClients" [generatePublicClient; generatePrivateClient]

BuildTask.create "Restore" [clean] {
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
}

BuildTask.create "RestoreTests" [clean.IfNeeded] {
    !! "test/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
}

BuildTask.create "Serve" [] {
    Npm.run "serve" id
}

BuildTask.create "Build" [] {
    Npm.run "build" id
}

BuildTask.create "TestIntegrations" [] {
    let result = DotNet.exec id "run" "--project src/IntegrationTests/IntegrationTests.fsproj"
    if not result.OK then failwithf "Error! %A" result.Errors
}

BuildTask.create "PublishIntegrationTests" [] {
    Trace.trace "Publishing Integration Tests..."
    // ref: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
    let runtime =
        if Environment.isLinux then "linux-x64"
        elif Environment.isWindows then "win-x64"
        elif Environment.isMacOS then "osx-x64"
        else failwithf "environment not supported"
    DotNet.publish (fun args ->
        { args with
            OutputPath = Some "src/IntegrationTests/out"
            Runtime = Some runtime })
        "src/IntegrationTests/IntegrationTests.fsproj"
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default