#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake
open System

let sln = __SOURCE_DIRECTORY__ </> "VehicleApi.sln"

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
    !! (genDir </> "Vehicle*.cs")
    |> Shell.copyFiles targetDir
}

BuildTask.create "UpdateProtos" [copyGenerated] {
    DotNet.build 
        (fun ops -> { ops with Configuration = DotNet.Debug })
        "src/Proto/Proto.csproj"
}

BuildTask.create "Restore" [] {
    DotNet.restore id sln
}

let unitTests = BuildTask.create "UnitTests" [] {
    Trace.trace "Running unit tests..."
    let result = DotNet.exec id "run" "--project src/UnitTests/UnitTests.fsproj"
    if not result.OK then failwith "Error!"
}

BuildTask.create "IntegrationTests" [] {
    Trace.trace "Running integration tests..."
    let result = DotNet.exec id "run" "--project src/IntegrationTests/IntegrationTests.fsproj"
    if not result.OK then failwith "Error!"
}

BuildTask.create "Publish" [unitTests] {
    Trace.trace "Publishing..."
    // ref: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
    let runtime =
        if Environment.isLinux then "linux-musl-x64"
        elif Environment.isWindows then "win-x64"
        else failwithf "environment not supported"
    // ref: https://www.hanselman.com/blog/MakingATinyNETCore30EntirelySelfcontainedSingleExecutable.aspx
    let customParams =
        [ "/p:PublishSingleFile=true"
          "/p:PublishTrimmed=true"
          sprintf "/p:RuntimeIdentifier=%s" runtime ]
    let customParamsStr = String.Join(" ", customParams)
    DotNet.publish (fun args ->
        { args with
            OutputPath = Some "src/Vehicle/out"
            Common =
                args.Common
                |> DotNet.Options.withCustomParams (Some customParamsStr) })
        "src/Vehicle/Vehicle.fsproj"
}

BuildTask.create "Serve" [] {
    DotNet.exec id "run" "--project src/Vehicle/Vehicle.fsproj"
    |> ignore
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
