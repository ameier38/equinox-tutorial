#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake

let sln = __SOURCE_DIRECTORY__ </> "VehicleApi.sln"

BuildTask.create "CleanAll" [] {
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
        __SOURCE_DIRECTORY__ // lease-api
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

BuildTask.create "Test" [] {
    Trace.trace "Running tests..."
    let result = DotNet.exec id "run" "--project src/Tests/Tests.fsproj"
    if not result.OK then failwith "Error!"
}

BuildTask.create "PublishTests" [] {
    Trace.trace "Publishing Tests..."
    DotNet.publish
        (fun args -> { args with OutputPath = Some "src/Tests/out"})
        "src/Tests/Tests.fsproj"
}

BuildTask.create "Publish" [] {
    Trace.trace "Publishing Lease API..."
    DotNet.publish
        (fun args -> { args with OutputPath = Some "src/Lease/out"})
        "src/Lease/Lease.fsproj"
}

BuildTask.create "Serve" [] {
    DotNet.exec id "run" "--project src/Lease/Lease.fsproj"
    |> ignore
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
