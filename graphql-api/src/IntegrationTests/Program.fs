open Expecto
open Expecto.Flip
open PrivateClient
open PublicClient
open Microsoft.IdentityModel.Tokens
open Shared
open System
open System.IO
open System.IdentityModel.Tokens
open System.Net.Http
open System.Security.Claims
open System.Security.Cryptography
open System.Text.RegularExpressions

let host = Env.getEnv "GRAPHQL_HOST" "localhost" 
let port = Env.getEnv "GRAPHQL_PORT" "4000" |> int
// let url = sprintf "http://%s:%i" host port
let url = "https://graphql.cosmicdealership.com"
let secretsDir = Env.getEnv "SECRETS_DIR" "/dev/secrets/cosmicdealership"
let privateKeyPath = Path.Join(secretsDir, "oauth", "private-key.pem")

type TokenFactory(privateKeyPath:string) =
    let tokenHandler = Jwt.JwtSecurityTokenHandler()
    let privateKey = 
        if not (File.Exists(privateKeyPath)) then failwithf "%s does not exist" privateKeyPath
        else File.ReadAllText(privateKeyPath)
    let pattern = @"-----BEGIN RSA PRIVATE KEY-----(.+)-----END RSA PRIVATE KEY-----"
    let signingKey =
        match Regex.Match(privateKey, pattern, RegexOptions.Singleline) with
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
                issuer = "https://cosmicdealership.us.auth0.com/",
                audience = "https://cosmicdealership.com",
                claims = claims,
                notBefore = Nullable(now),
                expires = Nullable(now.AddDays(2.0)),
                signingCredentials = signingCreds)
        // let token = tokenHandler.WriteToken(token)
        let token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Ijk2M1pCSl8zVEg1ZklYVzZjazJ0OCJ9.eyJpc3MiOiJodHRwczovL2Nvc21pY2RlYWxlcnNoaXAudXMuYXV0aDAuY29tLyIsInN1YiI6ImF1dGgwfDVmYTQyYmVhOWJhNDUzMDA2OWVjNzkxMyIsImF1ZCI6WyJodHRwczovL2Nvc21pY2RlYWxlcnNoaXAuY29tIiwiaHR0cHM6Ly9jb3NtaWNkZWFsZXJzaGlwLnVzLmF1dGgwLmNvbS91c2VyaW5mbyJdLCJpYXQiOjE2MDU2NDc1MDEsImV4cCI6MTYwNTczMzkwMSwiYXpwIjoib1FEWTF5dEMxekJraEc5R01TQlZVQXlwb3dWeEJkWVciLCJzY29wZSI6Im9wZW5pZCBwcm9maWxlIGVtYWlsIiwicGVybWlzc2lvbnMiOlsiYWRkOnZlaGljbGVzIiwiZ2V0OnZlaGljbGVzIiwibGVhc2U6dmVoaWNsZSIsImxpc3Q6dmVoaWNsZXMiLCJyZW1vdmU6dmVoaWNsZXMiLCJyZXR1cm46dmVoaWNsZXMiXX0.m-rQROQns5mULWOUWLpyU2JLiZbDkGWSJkNneKFskvg2ZpNR2Ut4XeoDvyfWNYDU5f7uipk8f8Q3mlD-gSG0tXP2ILdcKy_FWoWaGXiQMrR-tPudhi9XKS3avyrPk3dvXTtzbYBIrWDe77Yho7eoN0Cwob28os0z2WJRrk7g-hZAJlKPWEgdomra_k-iPdDxqHb5U-m5uIQ9q1ASbCSDJ9iJuHkcQYSEFzPT5SwRgChL5oQ7BWW2WoUEe9xpG6cl21WydJUau7qbgqexpuTcVv8IGtenXa-NAS-X4LT2Czs9AuVWEKx25UJpmBkSCp6lt-qxPvWIKMIO0vvAVfwihA"
        printfn "token: %s" token
        token

let tokenFactory = TokenFactory(privateKeyPath)

let getPrivateClient (permissions:string list) =
    let token = tokenFactory.GenerateToken(permissions)
    let bearer = sprintf "Bearer %s" token
    let httpClient = new HttpClient()
    httpClient.DefaultRequestHeaders.Add("Authorization", bearer)
    PrivateGraphqlClient(url, httpClient)

let getPublicClient () =
    let httpClient = new HttpClient()
    PublicGraphqlClient(url, httpClient)

let sleep = Async.Sleep 1000

let testAddVehicle =
    testAsync "add vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"]
        let client = getPrivateClient permissions
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
                  status = PrivateClient.VehicleStatus.Available }
            actualVehicle
            |> Expect.equal "should equal expected vehicle" expectedVehicle
        | error -> failwithf "error: %A" error
    }

let testUpdateVehicle =
    testAsync "update vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"; "update:vehicles"]
        let client = getPrivateClient permissions
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
                  status = PrivateClient.VehicleStatus.Available }
            actualVehicle
            |> Expect.equal "should equal expected vehicle" expectedVehicle
        | error -> failwithf "error: %A" error
    }

let testRemoveVehicle =
    testAsync "remove vehicle" {
        let permissions = ["add:vehicles"; "get:vehicles"; "remove:vehicles"]
        let client = getPrivateClient permissions
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
