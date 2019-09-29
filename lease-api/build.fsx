#load "./.fake/build.fsx/intellisense.fsx"

open BlackFox.Fake
open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

let solution = __SOURCE_DIRECTORY__ </> "Lease.sln"

BuildTask.create "Clean" [] {
    let directories =
        !! "**/Proto/out"
        ++ "**/Proto/bin"
        ++ "**/Proto/obj"
    Shell.cleanDirs directories
}

BuildTask.create "CopyGenerated" [] {
    let genDir =
        __SOURCE_DIRECTORY__ // lease-api
        |> Path.getDirectory // equinox-tutorial
        </> "protos" </> "gen" </> "csharp"
    let targetDir = __SOURCE_DIRECTORY__ </> "src" </> "Proto" </> "gen"
    targetDir |> Shell.cleanDir
    !! (genDir </> "*.cs")
    |> Shell.copyFiles targetDir
}

BuildTask.create "Restore" [] {
    Trace.trace "Restoring solution..."
    DotNet.restore id solution
}

BuildTask.create "Test" [] {
    Trace.trace "Running tests..."
    let result = DotNet.exec id "run" "--project src/Tests/Tests.fsproj"
    if not result.OK then failwith "Error!"
}

BuildTask.create "Publish" [] {
    Trace.trace "Publishing solution..."
    DotNet.publish 
        (fun args -> { args with OutputPath = Some "out"})
        solution
}

BuildTask.create "Serve" [] {
    DotNet.exec id "run" "--project src/Lease/Lease.fsproj"
    |> ignore
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
