open Expecto
open Expecto.Flip
open Generated
open Shared
open System
open System.IdentityModel.Tokens
open System.Net.Http

let host = Env.getEnv "GRAPHQL_API_HOST" "localhost" 
let port = Env.getEnv "GRAPHQL_API_PORT" "4000" |> int 
let url = sprintf "http://%s:%i" host port
let tokenHandler = Jwt.JwtSecurityTokenHandler()

// ref: jwt.io; add a 'permissions' array claim to the payload with desired permissions (e.g. "add:vehicles")
// {
//   "sub": "1234567890",
//   "name": "John Doe",
//   "iat": 1516239022,
//   "permissions": [
//      "get:vehicles",
//      "list:vehicles",
//      "add:vehicles",
//      "remove:vehicles",
//      "update:vehicles"
//   ]
// }
let token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJwZXJtaXNzaW9ucyI6WyJnZXQ6dmVoaWNsZXMiLCJsaXN0OnZlaGljbGVzIiwiYWRkOnZlaGljbGVzIiwicmVtb3ZlOnZlaGljbGVzIiwidXBkYXRlOnZlaGljbGVzIl19.KXWq8oGBi5Oki4i68lP0XCLSDwCrP7qE8Av2hsqXUTE"

let bearer = sprintf "Bearer %s" token
let httpClient = new HttpClient()
httpClient.DefaultRequestHeaders.Add("Authorization", bearer)
let gql = GeneratedGraphqlClient(url, httpClient)


let testAddVehicle =
    testAsync "add vehicle" {
        let vehicleId = Guid.NewGuid().ToString("N")
        let input: AddVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = "Falcon"
                  model = "9"
                  year = 2016 }}
        let! res = gql.AddVehicleAsync(input)
        res
        |> Expect.isOk "should successfully add vehicle"
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        let! res = gql.GetVehicleAsync(input)
        let expectedVehicleState:GetVehicle.VehicleState =
            { __typename = "VehicleState"
              vehicleId = vehicleId
              make = "Falcon"
              model = "9"
              year = 2016
              status = "Available" }
        let expectedResponse =
            { GetVehicle.Query.getVehicle =
                GetVehicle.GetVehicleResponse.VehicleState expectedVehicleState }
            |> Ok
        res
        |> Expect.equal "should equal expected response" expectedResponse
    }

let testUpdateVehicle =
    testAsync "update vehicle" {
        let vehicleId = Guid.NewGuid().ToString("N")
        let input: AddVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = "Falcon"
                  model = "9"
                  year = 2016 }}
        let! res = gql.AddVehicleAsync(input)
        res
        |> Expect.isOk "should successfully add vehicle"
        let input: UpdateVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = Some "Hawk"
                  model = Some "10"
                  year = None }}
        let! res = gql.UpdateVehicleAsync(input)
        res
        |> Expect.isOk "should successfully update vehicle"
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        let! res = gql.GetVehicleAsync(input)
        let expectedVehicleState:GetVehicle.VehicleState =
            { __typename = "VehicleState"
              vehicleId = vehicleId
              make = "Hawk"
              model = "10"
              year = 2016
              status = "Available" }
        let expectedResponse =
            { GetVehicle.Query.getVehicle =
                GetVehicle.GetVehicleResponse.VehicleState expectedVehicleState }
            |> Ok
        res
        |> Expect.equal "should equal expected response" expectedResponse
    }

let testRemoveVehicle =
    testAsync "remove vehicle" {
        let vehicleId = Guid.NewGuid().ToString("N")
        let input: AddVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = "Falcon"
                  model = "9"
                  year = 2016 }}
        let! res = gql.AddVehicleAsync(input)
        res
        |> Expect.isOk "should successfully add vehicle"
        let input: RemoveVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId }}
        let! res = gql.RemoveVehicleAsync(input)
        res
        |> Expect.isOk "should successfully remove vehicle"
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        let! res = gql.GetVehicleAsync(input)
        let expectedVehicleState:GetVehicle.VehicleState =
            { __typename = "VehicleState"
              vehicleId = vehicleId
              make = "Falcon"
              model = "9"
              year = 2016
              status = "Removed" }
        let expectedResponse =
            { GetVehicle.Query.getVehicle =
                GetVehicle.GetVehicleResponse.VehicleState expectedVehicleState }
            |> Ok
        res
        |> Expect.equal "should equal expected response" expectedResponse
    }

let tests =
    testList "All" [
        testAddVehicle
        testUpdateVehicle
        testRemoveVehicle
    ]

[<EntryPoint>]
let main argv =
    runTestsWithArgs defaultConfig argv tests
