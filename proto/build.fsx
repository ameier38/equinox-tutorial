#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.IO.FileSystemOperators
open Fake.Core.TargetOperators

let protoDir = __SOURCE_DIRECTORY__ + "/protos"

let run (workDir:string) (command:string) (args:string list) =
    let result =
        CreateProcess.fromRawCommand command args
        |> CreateProcess.withWorkingDirectory workDir
        |> CreateProcess.redirectOutput
        |> Proc.run
    if result.ExitCode = 0 then 
        result.Result.Output.Trim()
    else
        let output = result.Result.Output.Trim()
        let error = result.Result.Error
        failwithf "%s %A failed:\n%s\n%s" command args error output

let prototool = run protoDir "prototool"

Target.create "Default" (fun _ ->
    Trace.trace "Proto")

Target.create "Format" (fun _ ->
    prototool ["format"; "-w"]
    |> Trace.trace)

Target.create "Lint" (fun _ ->
    prototool ["lint"]
    |> Trace.trace)

Target.create "Compile" (fun _ ->
    prototool ["compile"]
    |> Trace.trace)

Target.create "Generate" (fun _ ->
    prototool ["generate"; "--debug"]
    |> Trace.trace)

Target.create "Test" ignore

"Lint"
 ==> "Compile"
 ==> "Test"

"Test"
 ==> "Generate"

Target.runOrDefault "Default"
