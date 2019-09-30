#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake

let solution = __SOURCE_DIRECTORY__ </> "Graphql.sln"

let cleanProto = BuildTask.create "CleanProto" [] {
    let directories =
        !! "**/Proto/out"
        ++ "**/Proto/bin"
        ++ "**/Proto/obj"
    Shell.cleanDirs directories
}

BuildTask.create "CopyGenerated" [cleanProto] {
    let genDir =
        __SOURCE_DIRECTORY__ // graphql-api
        |> Path.getDirectory // equinox-tutorial
        </> "protos" </> "gen" </> "csharp"
    let targetDir = __SOURCE_DIRECTORY__ </> "src" </> "Proto" </> "gen"
    !!(genDir </> "*.cs")
    |> Shell.copyFiles targetDir
}

BuildTask.create "Test" [] {
    Trace.trace "Running tests..."
    let result = DotNet.exec id "run" "--project src/Tests/Tests.fsproj"
    if not result.OK then failwithf "Error!"
}

BuildTask.create "Restore" [] {
    Trace.trace "Restoring solution..."
    DotNet.restore id solution
}

BuildTask.create "Publish" [] {
    Trace.trace "Publishing solution..."
    DotNet.publish 
        (fun args -> { args with OutputPath = Some "out"})
        solution
}

BuildTask.create "Serve" [] {
    DotNet.exec id "run" "--project src/Graphql/Graphql.fsproj"
    |> ignore
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
