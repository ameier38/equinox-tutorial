#load ".fake/build.fsx/intellisense.fsx"
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.JavaScript
open BlackFox.Fake

let clean = BuildTask.create "Clean" [] {
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
}

BuildTask.create "Restore" [clean.IfNeeded] {
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
}

let install = BuildTask.create "Install" [] {
    Npm.install id
}

let serve = BuildTask.create "Serve" [] {
    Npm.run "start" id
}

let build = BuildTask.create "Build" [] {
    Npm.run "build" id
}

BuildTask.create "TestIntegrations" [] {
    let result = DotNet.exec id "run" "src/IntegrationTests/IntegrationTests.fsproj"
    if not result.OK then failwithf "Error! %A" result.Errors
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default
