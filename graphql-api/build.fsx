#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake

let solution = __SOURCE_DIRECTORY__ </> "Solution.sln"

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
        __SOURCE_DIRECTORY__ // graphql-api
        |> Path.getDirectory // equinox-tutorial
        </> "protos" </> "gen" </> "csharp"
    let targetDir = __SOURCE_DIRECTORY__ </> "src" </> "Proto" </> "gen"
    !!(genDir </> "*.cs")
    |> Shell.copyFiles targetDir
}

BuildTask.create "UpdateProtos" [copyGenerated] {
    DotNet.build 
        (fun ops -> { ops with Configuration = DotNet.Debug })
        "src/Proto/Proto.csproj"
}

BuildTask.create "Restore" [] {
    Trace.trace "Restoring solution..."
    DotNet.restore id solution
}

BuildTask.create "Test" [] {
    Trace.trace "Running unit tests..."
    let result = DotNet.exec id "run" "--project src/Tests/Tests.fsproj"
    if not result.OK then failwithf "Error! %A" result.Errors
}

BuildTask.create "Publish" [] {
    Trace.trace "Publishing GraphQL API..."
    DotNet.publish
        (fun args -> { args with OutputPath = Some "src/Server/out"})
        "src/Server/Server.fsproj"
}

BuildTask.create "Serve" [] {
    DotNet.exec id "run" "--project src/Server/Server.fsproj"
    |> ignore
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
