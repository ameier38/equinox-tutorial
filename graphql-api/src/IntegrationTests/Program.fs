open Expecto
open Expecto.Flip
open PrivateClient
open PublicClient
open Microsoft.IdentityModel.Tokens
open Shared
open System
open System.IdentityModel.Tokens
open System.Net.Http
open System.Security.Claims
open System.Text

let host = Env.getEnv "GRAPHQL_API_HOST" "localhost" 
let port = Env.getEnv "GRAPHQL_API_PORT" "4000" |> int
let url = sprintf "http://%s:%i" host port

// NB: You can also use jwt.io; add a 'permissions' array claim to the payload with desired permissions (e.g. "add:vehicles")
let generateToken (permissions:string list) =
    let tokenHandler = Jwt.JwtSecurityTokenHandler()
    let claims = [| for permission in permissions -> Claim("permissions", permission) |]
    // NB: see README to generate the test authentication key
    let signingKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes("671f54ce0c540f78ffe1e26dcf9c2a047aea4fda"))
    let signingCreds =
        SigningCredentials(
            key = signingKey,
            algorithm = SecurityAlgorithms.HmacSha256)
    let token =
        Jwt.JwtSecurityToken(
            issuer = "https://cosmicdealership.auth0.com/",
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
    PrivateGraphqlClient(url, httpClient)

let getPublicClient () =
    let httpClient = new HttpClient()
    PublicGraphqlClient(url, httpClient)

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
        match! client.AddVehicleAsync(input) with
        | Ok ({ addVehicle = AddVehicle.AddVehicleResponse.Success { message = msg } }) -> printfn "success: %s" msg
        | error -> failwithf "error: %A" error
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        match! client.GetVehicleAsync(input) with
        | Ok { getVehicle = GetVehicle.GetVehicleResponse.InventoriedVehicle actualVehicle } ->
            let expectedVehicle:GetVehicle.InventoriedVehicle =
                { __typename = "InventoriedVehicle"
                  vehicleId = vehicleId
                  vehicle = {
                      make = "Falcon"
                      model = "9"
                      year = 2016
                  }
                  status = PrivateClient.VehicleStatus.Available }
            actualVehicle
            |> Expect.equal "should equal expected vehicle" expectedVehicle
        | error -> failwithf "error: %A" error
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
        match! client.AddVehicleAsync(input) with
        | Ok { addVehicle = AddVehicle.AddVehicleResponse.Success { message = msg } } -> printfn "success: %s" msg
        | error -> failwithf "error: %A" error
        let input: UpdateVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId
                  make = "Hawk"
                  model = "10"
                  year = 2016 }}
        match! client.UpdateVehicleAsync(input) with
        | Ok { updateVehicle = UpdateVehicle.UpdateVehicleResponse.Success { message = msg }} -> printfn "success: %s" msg
        | error -> failwithf "error: %A" error
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        match! client.GetVehicleAsync(input) with
        | Ok { getVehicle = GetVehicle.GetVehicleResponse.InventoriedVehicle actualVehicle } ->
            let expectedVehicle:GetVehicle.InventoriedVehicle =
                { __typename = "InventoriedVehicle"
                  vehicleId = vehicleId
                  vehicle = {
                      make = "Hawk"
                      model = "10"
                      year = 2016
                  }
                  status = PrivateClient.VehicleStatus.Available }
            actualVehicle
            |> Expect.equal "should equal expected vehicle" expectedVehicle
        | error -> failwithf "error: %A" error
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
        match! client.AddVehicleAsync(input) with
        | Ok { addVehicle = AddVehicle.AddVehicleResponse.Success { message = msg }} -> printfn "success: %s" msg
        | error -> failwithf "error: %A" error
        let input: RemoveVehicle.InputVariables =
            { input =
                { vehicleId = vehicleId }}
        match! client.RemoveVehicleAsync(input) with
        | Ok { removeVehicle = RemoveVehicle.RemoveVehicleResponse.Success { message = msg }} -> printfn "success: %s" msg
        | error -> failwithf "error: %A" error
        do! Async.Sleep(2000)
        let input: GetVehicle.InputVariables =
            { input = { vehicleId = vehicleId }}
        match! client.GetVehicleAsync(input) with
        | Ok { getVehicle = GetVehicle.GetVehicleResponse.VehicleNotFound { message = msg }} ->
            msg
            |> Expect.equal "should equal expected message" (sprintf "Vehicle-%s not found" vehicleId)
        | error -> failwithf "error: %A" error
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
