open Expecto
open Expecto.Flip
open FSharp.Data.GraphQL
open Shared
open System.IdentityModel.Tokens

let host = Some "localhost" |> Env.getEnv "GRAPHQL_API_HOST"
let port = Some "4000" |> Env.getEnv "GRAPHQL_API_PORT" |> int 
let url = sprintf "http://%s:%d" host port
let tokenHandler = Jwt.JwtSecurityTokenHandler()

// ref: jwt.io; add in the correct permissions to the payload
let token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJwZXJtaXNzaW9ucyI6WyJhZGQ6dmVoaWNsZXMiXX0.Xxa0HXMjdx5azgrEsD974CzdagihFAbMQvH5XWWZMuQ"

type GraphQLClient = GraphQLProvider<"http://localhost:4000">

let addVehicle =
    GraphQLClient.Operation<"""
    mutation AddVehicle($input:AddVehicleInput!) {
        addVehicle(input: $input)
    }
    """>()

let testAddVehicle =
    test "addVehicle" {
        let bearer = sprintf "Bearer %s" token
        let headers = ["Authorization", bearer]
        let input =
            GraphQLClient.Types.AddVehicleInput(
                make = "Falcon",
                model = "9",
                year = 2016)
        let ctx = GraphQLClient.GetContext(httpHeaders=headers)
        let res = addVehicle.Run(ctx, input)
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
