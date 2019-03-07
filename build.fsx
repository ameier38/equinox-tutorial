#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target 
nuget FSharp.Core 4.5.0.0 //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.FileSystemOperators

let paketFile = if Environment.isUnix then "paket" else "paket.exe"
let paketExe = __SOURCE_DIRECTORY__ </> ".paket" </> paketFile
let solution = __SOURCE_DIRECTORY__ </> "Tutorial.sln"

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
    DotNet.exec id "run" "--project src/Lease/Lease.fsproj"
    |> ignore)

open Fake.Core.TargetOperators

"InstallPaket"
 ==> "InstallDependencies"

"InstallDependencies"
 ==> "Restore"

"InstallDependencies"
 ==> "Build"

Target.runOrDefault "Default"
