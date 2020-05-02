#r "paket:
nuget BlackFox.Fake.BuildTask
nuget Fake.DotNet.Cli
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"
#load ".fake/build.fsx/intellisense.fsx"
open BlackFox.Fake
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open System.IO

let vendorDir = __SOURCE_DIRECTORY__ </> "vendor"
let protoDir = __SOURCE_DIRECTORY__ </> "proto"
let genDir = __SOURCE_DIRECTORY__ </> "gen"
let googleApisCommit = "d4aa417ed2bba89c2d216900282bddfdafef6128"
let googleApisDir = vendorDir </> "github.com" </> "googleapis" </> "googleapis"

let run (command:string) (args:string list) (workDir:string) =
    CreateProcess.fromRawCommand command args
    |> CreateProcess.withWorkingDirectory workDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let buf (args: string list) (workDir:string) =
    run "buf" args workDir

let protoc (args:string list) (workDir:string) =
    run "protoc" args workDir

let git (args:string list) (workDir:string) =
    run "git" args workDir

let cloneGoogleApis = BuildTask.create "CloneGoogleApis" [] {
    if not (Directory.Exists(googleApisDir)) then
        __SOURCE_DIRECTORY__
        |> git ["clone"; "https://github.com/googleapis/googleapis.git"; googleApisDir]
    googleApisDir
    |> git ["reset"; "--hard"; googleApisCommit]
}

let cleanVendor = BuildTask.create "CleanVendor" [] {
    Shell.cleanDir vendorDir
}

let lint = BuildTask.create "Lint" [] {
    __SOURCE_DIRECTORY__
    |> buf ["check"; "lint"]
}

let protos = !! (protoDir </> "**/*.proto")
let protoPaths = [ googleApisDir; protoDir ]

let generateCsharp = BuildTask.create "GenerateCsharp" [ lint ] {
    let csharpGenDir = genDir </> "csharp"
    csharpGenDir |> Directory.ensure
    csharpGenDir |> Shell.cleanDir
    let csharpArgs =
        [ "--plugin=protoc-gen-grpc=/usr/local/bin/grpc_csharp_plugin"
          sprintf "--grpc_out=%s" (genDir </> "csharp")
          sprintf "--csharp_out=%s" (genDir </> "csharp") ]
    let pathArgs =
        [ for path in protoPaths ->
            sprintf "--proto_path=%s" path ]
    __SOURCE_DIRECTORY__
    |> protoc [yield! pathArgs; yield! csharpArgs; yield! protos ]
}

let generateGo = BuildTask.create "GenerateGo" [ lint ] {
    let goGenDir = genDir </> "go"
    goGenDir |> Directory.ensure
    goGenDir |> Shell.cleanDir
    let goArgs =
        [ "--plugin=protoc-gen-go-grpc=/root/go/bin/protoc-gen-go"
          sprintf "--go-grpc_out=%s" (genDir </> "go") ]
    let pathArgs =
        [ for path in protoPaths ->
            sprintf "--proto_path=%s" path ]
    __SOURCE_DIRECTORY__
    |> protoc [yield! pathArgs; yield! goArgs; yield! protos ]
}

let generate = BuildTask.createEmpty "Generate" [ generateCsharp; generateGo ]

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
