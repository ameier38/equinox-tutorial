open Expecto
open Expecto.Flip
open PrivateClient
open Microsoft.IdentityModel.Tokens
open Shared
open System
open System.IdentityModel.Tokens
open System.Net.Http
open System.Security.Claims
open System.Text

let publicHost = Env.getEnv "PUBLIC_GRAPHQL_API_HOST" "localhost" 
let publicPort = Env.getEnv "PUBLIC_GRAPHQL_API_PORT" "4000" |> int
let publicUrl = sprintf "http://%s:%i" publicHost publicPort
let privateHost = Env.getEnv "PRIVATE_GRAPHQL_API_HOST" "localhost"
let privatePort = Env.getEnv "PRIVATE_GRAPHQL_API_PORT" "4001" |> int 
let privateUrl = sprintf "http://%s:%i" privateHost privatePort

// NB: You can also use jwt.io; add a 'permissions' array claim to the payload with desired permissions (e.g. "add:vehicles")
let generateToken (permissions:string list) =
    let tokenHandler = Jwt.JwtSecurityTokenHandler()
    let claims = [| for permission in permissions -> Claim("permissions", permission) |]
    let signingKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes("671f54ce0c540f78ffe1e26dcf9c2a047aea4fda"))
    let signingCreds =
        SigningCredentials(
            key = signingKey,
            algorithm = SecurityAlgorithms.HmacSha256)
    let token =
        Jwt.JwtSecurityToken(
            issuer = "https://ameier38.auth0.com/",
            audience = "https://cosmicdealership.com",
            claims = claims,
            expires = Nullable(DateTime.UtcNow.AddHours(1.0)),
            signingCredentials = signingCreds)
    let token = tokenHandler.WriteToken(token)
    printfn "token: %s" token
    token

let getPrivateClient (permissions:string list) =
    let token = generateToken permissions
    let bearer = sprintf "Bearer %s" token
    let httpClient = new HttpClient()
    httpClient.DefaultRequestHeaders.Add("Authorization", bearer)
    PrivateGraphqlClient(privateUrl, httpClient)


let testAddVehicle =
    testAsync "add vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"]
        let client = getPrivateClient permissions
        let vehicleId = Guid.NewGuid().ToString("N")
        let input: AddVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = "Falcon"
                  model = "9"
                  year = 2016 }}
        let! res = client.AddVehicleAsync(input)
        res
        |> Expect.isOk "should successfully add vehicle"
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        let! res = client.GetVehicleAsync(input)
        let expectedVehicleState:GetVehicle.Vehicle =
            { __typename = "VehicleState"
              vehicleId = vehicleId
              make = "Falcon"
              model = "9"
              year = 2016
              status = "Available" }
        let expectedResponse =
            { GetVehicle.Query.getVehicle =
                GetVehicle.GetVehicleResponse.Vehicle expectedVehicleState }
            |> Ok
        res
        |> Expect.equal "should equal expected response" expectedResponse
    }

let testUpdateVehicle =
    testAsync "update vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"; "update:vehicles"]
        let client = getPrivateClient permissions
        let vehicleId = Guid.NewGuid().ToString("N")
        let input: AddVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = "Falcon"
                  model = "9"
                  year = 2016 }}
        let! res = client.AddVehicleAsync(input)
        res
        |> Expect.isOk "should successfully add vehicle"
        let input: UpdateVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = Some "Hawk"
                  model = Some "10"
                  year = None }}
        let! res = client.UpdateVehicleAsync(input)
        res
        |> Expect.isOk "should successfully update vehicle"
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        let! res = client.GetVehicleAsync(input)
        let expectedVehicleState:GetVehicle.Vehicle =
            { __typename = "VehicleState"
              vehicleId = vehicleId
              make = "Hawk"
              model = "10"
              year = 2016
              status = "Available" }
        let expectedResponse =
            { GetVehicle.Query.getVehicle =
                GetVehicle.GetVehicleResponse.Vehicle expectedVehicleState }
            |> Ok
        res
        |> Expect.equal "should equal expected response" expectedResponse
    }

let testRemoveVehicle =
    testAsync "remove vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"; "remove:vehicles"]
        let client = getPrivateClient permissions
        let vehicleId = Guid.NewGuid().ToString("N")
        let input: AddVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = "Falcon"
                  model = "9"
                  year = 2016 }}
        let! res = client.AddVehicleAsync(input)
        res
        |> Expect.isOk "should successfully add vehicle"
        let input: RemoveVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId }}
        let! res = client.RemoveVehicleAsync(input)
        res
        |> Expect.isOk "should successfully remove vehicle"
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        let! res = client.GetVehicleAsync(input)
        let expectedVehicleState:GetVehicle.Vehicle =
            { __typename = "VehicleState"
              vehicleId = vehicleId
              make = "Falcon"
              model = "9"
              year = 2016
              status = "Removed" }
        let expectedResponse =
            { GetVehicle.Query.getVehicle =
                GetVehicle.GetVehicleResponse.Vehicle expectedVehicleState }
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
