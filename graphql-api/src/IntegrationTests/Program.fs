open Expecto
open Expecto.Flip
open FSharp.Data.GraphQL
open Shared

let host = Some "localhost" |> Env.getEnv "GRAPHQL_API_HOST"
let port = Some "4000" |> Env.getEnv "GRAPHQL_API_PORT" |> int 
let url = sprintf "http://%s:%d" host port

type GraphQLClient = GraphQLProvider<"http://localhost:4000">

let addVehicle =
    GraphQLClient.Operation<"""
    mutation AddVehicle($input:AddVehicleInput!) {
        addVehicle(input: $input)
    }
    """>()

let testAddVehicle =
    test "addVehicle" {
        let input =
            GraphQLClient.Types.AddVehicleInput(
                make = "Falcon",
                model = "9",
                year = 2016)
        let res = addVehicle.Run(input)
        res.Data
        |> Expect.isSome "should not fail"
    }

let tests =
    testList "All" [
        testAddVehicle
    ]

[<EntryPoint>]
let main argv =
    runTestsWithArgs defaultConfig argv tests
