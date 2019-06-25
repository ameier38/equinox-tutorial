#r "paket: groupref Fake //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators

let rootDir = 
    __SOURCE_DIRECTORY__    // graphql-api
    |> Path.getDirectory    // equinox-tutorial  
let paketFile = if (Environment.isLinux || Environment.isMacOS) then "paket" else "paket.exe"
let paketExe = __SOURCE_DIRECTORY__ </> ".paket" </> paketFile
let solution = __SOURCE_DIRECTORY__ </> "Graphql.sln"

Target.create "Default" (fun _ ->
    Trace.trace "Equinox Tutorial")

Target.create "InstallPaket" (fun _ ->
    if not (File.exists paketExe) then
        DotNet.exec id "tool" "install --tool-path \".paket\" Paket --add-source https://api.nuget.org/v3/index.json"
        |> ignore
    else
        printfn "paket already installed")

Target.create "InstallDependencies" (fun _ ->
    let result =
        CreateProcess.fromRawCommand paketExe ["install"]
        |> Proc.run
    if result.ExitCode <> 0 then failwith "Failed to install dependencies")

Target.create "CleanProto" (fun _ ->
    let directories =
        !! "**/Proto/out"
        ++ "**/Proto/bin"
        ++ "**/Proto/obj"
    Shell.cleanDirs directories)

Target.create "CopyProto" (fun _ ->
    let leaseProto = rootDir </> "protobuf" </> "lease.proto"
    File.checkExists leaseProto
    let targetDir = __SOURCE_DIRECTORY__ </> "src" </> "Proto"
    leaseProto |> Shell.copyFile targetDir)

Target.create "BuildProto" (fun _ ->
    let protoProj = __SOURCE_DIRECTORY__ </> "src" </> "Proto" </> "Proto.csproj"
    DotNet.build id protoProj)

Target.create "Restore" (fun _ ->
    Trace.trace "Restoring solution..."
    DotNet.restore id solution)

Target.create "Test" (fun _ ->
    Trace.trace "Running tests..."
    DotNet.exec id "run" "--project src/Tests/Tests.fsproj"
    |> ignore)

Target.create "Build" (fun _ ->
    Trace.trace "Building solution..."
    DotNet.build id solution)

Target.create "Publish" (fun _ ->
    Trace.trace "Publishing solution..."
    DotNet.publish 
        (fun args -> { args with OutputPath = Some "out"})
        solution)

Target.create "Serve" (fun _ ->
    DotNet.exec id "run" "--project src/Graphql/Graphql.fsproj"
    |> ignore)

open Fake.Core.TargetOperators

"InstallPaket"
 ==> "InstallDependencies"

"CleanProto"
 ==> "BuildProto"

"CopyProto"
 ==> "BuildProto"

"InstallDependencies"
 ==> "Restore"

"Restore"
 ==> "Test"

"InstallDependencies"
 ==> "Build"

"InstallDependencies"
 ==> "Publish"

Target.runOrDefault "Default"