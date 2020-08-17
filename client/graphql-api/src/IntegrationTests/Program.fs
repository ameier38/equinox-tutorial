open Expecto
open Expecto.Flip
open Generated
open Shared
open System.IdentityModel.Tokens
open System.Net.Http

let host = Env.getEnv "GRAPHQL_API_HOST" "localhost" 
let port = Env.getEnv "GRAPHQL_API_PORT" "4000" |> int 
let url = sprintf "http://%s:%i" host port
let tokenHandler = Jwt.JwtSecurityTokenHandler()

// ref: jwt.io; add in the correct permissions to the payload
let token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJwZXJtaXNzaW9ucyI6WyJhZGQ6dmVoaWNsZXMiXX0.Xxa0HXMjdx5azgrEsD974CzdagihFAbMQvH5XWWZMuQ"

let testAddVehicle =
    test "add vehicle" {
        let bearer = sprintf "Bearer %s" token
        let httpClient = new HttpClient()
        do httpClient.DefaultRequestHeaders.Add("Authorization", bearer)
        let gql = GeneratedGraphqlClient(url, httpClient)
        let input =
            { make = "Falcon"
              model = "9"
              year = 2016 }
        let res = gql.AddVehicle({ input = input })
        res
        |> Expect.isOk "should successfull add vehicle"
    }

let tests =
    testList "All" [
        testAddVehicle
    ]

[<EntryPoint>]
let main argv =
    runTestsWithArgs defaultConfig argv tests
