#r "paket:
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.IO
open Fake.DotNet
open Fake.IO.FileSystemOperators

let paketExe = __SOURCE_DIRECTORY__ </> ".paket" </> "paket.exe"
let dogSolution = __SOURCE_DIRECTORY__ </> "Dog.sln"

Target.create "Default" (fun _ ->
    Trace.trace "Equinox Tutorial"
)

Target.create "InstallPaket" (fun _ ->
    if not (File.exists paketExe) then
        DotNet.exec id "tool" "install --tool-path \".paket\" Paket --add-source https://api.nuget.org/v3/index.json"
        |> ignore
    else
        printfn "paket already installed"
)

Target.create "InstallDependencies" (fun _ ->
    let result =
        CreateProcess.fromRawCommand paketExe ["install"]
        |> Proc.run
    if result.ExitCode <> 0 then failwith "Failed to install dependencies"
)

Target.create "Restore" (fun _ ->
    Trace.trace "Restoring solution..."
    DotNet.restore id dogSolution
)

Target.create "Serve" (fun _ ->
    DotNet.exec id "run" "--project src/Dog/Dog.fsproj"
    |> ignore
)

open Fake.Core.TargetOperators

"InstallDependencies"
 ==> "Restore"

Target.runOrDefault "Default"
