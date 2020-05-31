#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake
open System

BuildTask.create "Clean" [] {
    let directories =
        !! "**/out"
        ++ "**/bin"
        ++ "**/obj"
        ++ "**/gen"
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
        __SOURCE_DIRECTORY__ // vehicle-api
        |> Path.getDirectory // equinox-tutorial
        </> "protos" </> "gen" </> "csharp"
    let targetDir = __SOURCE_DIRECTORY__ </> "src" </> "Proto" </> "gen"
    targetDir |> Shell.cleanDir
    !! (genDir </> "*.cs")
    |> Shell.copyFiles targetDir
}

BuildTask.create "UpdateProtos" [copyGenerated] {
    DotNet.build 
        (fun ops -> { ops with Configuration = DotNet.Debug })
        "src/Proto/Proto.csproj"
}

BuildTask.create "Restore" [] {
    DotNet.restore id "src/Server/Server.fsproj"
}

let testUnits = BuildTask.create "TestUnits" [] {
    Trace.trace "Running unit tests..."
    let result = DotNet.exec id "run" "--project src/UnitTests/UnitTests.fsproj"
    if not result.OK then failwith "Error!"
}

BuildTask.create "TestIntegrations" [] {
    Trace.trace "Running integration tests..."
    let result = DotNet.exec id "run" "--project src/IntegrationTests/IntegrationTests.fsproj"
    if not result.OK then failwith "Error!"
}

BuildTask.create "Publish" [testUnits] {
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

BuildTask.create "Serve" [] {
    DotNet.exec id "run" "--project src/Server/Server.fsproj"
    |> ignore
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
