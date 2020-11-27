#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake

let run (command:string) (args:string list) =
    CreateProcess.fromRawCommand command args
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let snowflaqe = run "snowflaqe"

BuildTask.create "Clean" [] {
    let directories = 
        !! "**/out"
        ++ "**/bin"
        ++ "**/obj"
    Shell.cleanDirs directories
}

let cleanProto = BuildTask.create "CleanProto" [] {
    let directories =
        !! "**/Proto/out"
        ++ "**/Proto/bin"
        ++ "**/Proto/obj"
        ++ "**/Proto/gen"
    Shell.cleanDirs directories
}

let copyGenerated = BuildTask.create "CopyGenerated" [cleanProto] {
    let genDir =
        __SOURCE_DIRECTORY__ // graphql-api
        |> Path.getDirectory // equinox-tutorial
        </> "protos" </> "gen" </> "csharp"
    if not (DirectoryInfo.exists (DirectoryInfo.ofPath genDir)) then failwithf "%s does not exist" genDir
    let targetDir = __SOURCE_DIRECTORY__ </> "src" </> "Proto" </> "gen"
    !!(genDir </> "*.cs")
    |> Seq.map (fun f -> Trace.logf "copying file: %s" f; f)
    |> Shell.copyFiles targetDir
}

BuildTask.create "UpdateProtos" [copyGenerated] {
    DotNet.build 
        (fun ops -> { ops with Configuration = DotNet.Debug })
        "src/Proto/Proto.csproj"
}

BuildTask.create "Restore" [] {
    Trace.trace "Restoring..."
    [ "src/Server/Server.fsproj"
      "src/IntegrationTests/IntegrationTests.fsproj" ]
    |> List.iter (DotNet.restore id)
}

BuildTask.create "GenerateTestClient" [] {
    Trace.trace "Generating test client..."
    snowflaqe ["--generate"; "--config"; "snowflaqe.json"]
}

BuildTask.create "TestIntegrations" [] {
    Trace.trace "Running integration tests..."
    let result = DotNet.exec id "run" "--project src/IntegrationTests/IntegrationTests.fsproj"
    if not result.OK then failwithf "Error! %A" result.Errors
}

BuildTask.create "Publish" [] {
    Trace.trace "Publishing..."
    // ref: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
    let runtime =
        if Environment.isLinux then "linux-x64"
        elif Environment.isWindows then "win-x64"
        elif Environment.isMacOS then "osx-x64"
        else failwithf "environment not supported"
    DotNet.publish (fun args ->
        { args with
            OutputPath = Some "src/Server/out"
            Runtime = Some runtime })
        "src/Server/Server.fsproj"
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

BuildTask.create "Serve" [] {
    DotNet.exec id "watch" "--project src/Server/Server.fsproj run"
    |> ignore
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
