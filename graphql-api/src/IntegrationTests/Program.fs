open Expecto
open Expecto.Flip
open TestClient
open Microsoft.IdentityModel.Tokens
open Shared
open System
open System.IdentityModel.Tokens
open System.Net.Http
open System.Security.Claims
open System.Security.Cryptography
open System.Text.RegularExpressions

let graphqlConfig = Config.GraphqlConfig.Load()
let oauthConfig = Config.OAuthConfig.Load()

type TokenFactory(oauthConfig:Config.OAuthConfig) =
    let tokenHandler = Jwt.JwtSecurityTokenHandler()
    let pattern = @"-----BEGIN RSA PRIVATE KEY-----(.+)-----END RSA PRIVATE KEY-----"
    let signingKey =
        match Regex.Match(oauthConfig.PrivateKey.Value(), pattern, RegexOptions.Singleline) with
        | m when m.Success ->
            let rsa = RSA.Create()
            let contents = m.Groups.Item(1).Value.Replace("\n", String.Empty)
            let data = ReadOnlySpan(Convert.FromBase64String(contents))
            rsa.ImportRSAPrivateKey(data) |> ignore
            RsaSecurityKey(rsa)
        | _ -> failwithf "invalid private key"
    let signingCreds =
        SigningCredentials(
            key = signingKey,
            algorithm = SecurityAlgorithms.RsaSha256)
    
    member _.GenerateToken(permissions:string list) =
        let now = DateTime.UtcNow.Date
        let claims = [| for permission in permissions -> Claim("permissions", permission) |]
        let token =
            Jwt.JwtSecurityToken(
                issuer = oauthConfig.Issuer,
                audience = oauthConfig.Audience,
                claims = claims,
                notBefore = Nullable(now),
                expires = Nullable(now.AddDays(2.0)),
                signingCredentials = signingCreds)
        let token = tokenHandler.WriteToken(token)
        printfn "token: %s" token
        token

let tokenFactory = TokenFactory(oauthConfig)

let getClient (permissions:string list) =
    let token = tokenFactory.GenerateToken(permissions)
    let bearer = sprintf "Bearer %s" token
    let httpClient = new HttpClient()
    httpClient.DefaultRequestHeaders.Add("Authorization", bearer)
    TestGraphqlClient(graphqlConfig.Url, httpClient)

let sleep = Async.Sleep 1000

let testAddVehicle =
    testAsync "add vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"]
        let client = getClient permissions
        do! sleep
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
        do! sleep
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
                  status = VehicleStatus.Available }
            actualVehicle
            |> Expect.equal "should equal expected vehicle" expectedVehicle
        | error -> failwithf "error: %A" error
    }

let testUpdateVehicle =
    testAsync "update vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"; "update:vehicles"]
        let client = getClient permissions
        do! sleep
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
        do! sleep
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
                  status = VehicleStatus.Available }
            actualVehicle
            |> Expect.equal "should equal expected vehicle" expectedVehicle
        | error -> failwithf "error: %A" error
    }

let testRemoveVehicle =
    testAsync "remove vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"; "remove:vehicles"]
        let client = getClient permissions
        do! sleep
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
        do! sleep
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