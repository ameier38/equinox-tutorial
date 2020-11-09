open Expecto
open Expecto.Flip
open Grpc.Core
open Shared
open System
open System.Threading

let vehicleProcessorHost = Env.getEnv "VEHICLE_PROCESSOR_HOST" "vehicle-processor"
let vehicleProcessorPort = Env.getEnv "VEHICLE_PROCESSOR_PORT" "50051" |> int
let vehicleProcessorChannelTarget = sprintf "%s:%d" vehicleProcessorHost vehicleProcessorPort
let vehicleProcessorChannel = Channel(vehicleProcessorChannelTarget, ChannelCredentials.Insecure)
let vehicleCommandService = CosmicDealership.Vehicle.V1.VehicleCommandService.VehicleCommandServiceClient(vehicleProcessorChannel)

let vehicleReaderHost = Env.getEnv "VEHICLE_READER_HOST" "vehicle-reader"
let vehicleReaderPort = Env.getEnv "VEHICLE_READER_PORT" "50051" |> int
let vehicleReaderChannelTarget = sprintf "%s:%d" vehicleReaderHost vehicleReaderPort
let vehicleReaderChannel = Channel(vehicleReaderChannelTarget, ChannelCredentials.Insecure)
let vehicleQueryService = CosmicDealership.Vehicle.V1.VehicleQueryService.VehicleQueryServiceClient(vehicleReaderChannel)

let createVehicleId () = Guid.NewGuid().ToString("N")

let createUser (permissions:string list) =
    let user = CosmicDealership.User.V1.User(UserId="test")
    user.Permissions.AddRange(permissions)
    user

let addVehicle (user:CosmicDealership.User.V1.User) (vehicleId:string) (vehicle:CosmicDealership.Vehicle.V1.Vehicle) =
    let req = CosmicDealership.Vehicle.V1.AddVehicleRequest(User=user, VehicleId=vehicleId, Vehicle=vehicle)
    vehicleCommandService.AddVehicle(req)

let updateVehicle (user:CosmicDealership.User.V1.User) (vehicleId:string) (vehicle:CosmicDealership.Vehicle.V1.Vehicle) =
    let req = CosmicDealership.Vehicle.V1.UpdateVehicleRequest(User=user, VehicleId=vehicleId, Vehicle = vehicle)
    vehicleCommandService.UpdateVehicle(req)

let addImage (user:CosmicDealership.User.V1.User) (vehicleId:string) (imageUrl:string) =
    let req = CosmicDealership.Vehicle.V1.AddImageRequest(User=user, VehicleId=vehicleId, ImageUrl=imageUrl)
    vehicleCommandService.AddImage(req)

let removeVehicle (user:CosmicDealership.User.V1.User) (vehicleId:string) =
    let req = CosmicDealership.Vehicle.V1.RemoveVehicleRequest(User=user, VehicleId=vehicleId)
    vehicleCommandService.RemoveVehicle(req)

let leaseVehicle (user:CosmicDealership.User.V1.User) (vehicleId:string) =
    let req = CosmicDealership.Vehicle.V1.LeaseVehicleRequest(User=user, VehicleId=vehicleId)
    vehicleCommandService.LeaseVehicle(req)

let returnVehicle (user:CosmicDealership.User.V1.User) (vehicleId:string) =
    let req = CosmicDealership.Vehicle.V1.ReturnVehicleRequest(User=user, VehicleId=vehicleId)
    vehicleCommandService.ReturnVehicle(req)

type ListVehiclesResponseCase = CosmicDealership.Vehicle.V1.ListVehiclesResponse.ResponseOneofCase

let listVehicles (user:CosmicDealership.User.V1.User) =
    let rec recurse (pageToken:string option) =
        seq {
            let req =
                match pageToken with
                | Some pageToken ->
                    CosmicDealership.Vehicle.V1.ListVehiclesRequest(User=user, PageToken=pageToken, PageSize=Nullable(1))
                | None ->
                    CosmicDealership.Vehicle.V1.ListVehiclesRequest(User=user, PageSize=Nullable(1))
            let res = vehicleQueryService.ListVehicles(req)
            match res.ResponseCase with
            | ListVehiclesResponseCase.Success ->
                match res.Success.NextPageToken with
                | "" -> yield! res.Success.Vehicles
                | nextPageToken ->
                    yield! res.Success.Vehicles
                    yield! recurse (Some nextPageToken)
            | _ -> failwith "error!"
        }
    recurse None

type ListAvailableVehiclesResponseCase = CosmicDealership.Vehicle.V1.ListAvailableVehiclesResponse.ResponseOneofCase

let listAvailableVehicles () =
    let rec recurse (pageToken:string option) =
        seq {
            let req =
                match pageToken with
                | Some pageToken ->
                    CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest(
                        PageToken=pageToken,
                        PageSize=Nullable(1))
                | None ->
                    CosmicDealership.Vehicle.V1.ListAvailableVehiclesRequest(
                        PageSize=Nullable(1))
            let res = vehicleQueryService.ListAvailableVehicles(req)
            match res.ResponseCase with
            | ListAvailableVehiclesResponseCase.Success ->
                match res.Success.NextPageToken with
                | "" -> yield! res.Success.Vehicles
                | nextPageToken ->
                    yield! res.Success.Vehicles
                    yield! recurse (Some nextPageToken)
            | _ -> failwith "error!"
        }
    recurse None

type GetVehicleResponseCase = CosmicDealership.Vehicle.V1.GetVehicleResponse.ResponseOneofCase

let getVehicle (user:CosmicDealership.User.V1.User) (vehicleId:string) =
    let req = CosmicDealership.Vehicle.V1.GetVehicleRequest(User=user, VehicleId=vehicleId)
    vehicleQueryService.GetVehicle(req)

let testAddVehicle =
    test "add vehicle" {
        let user = createUser ["add:vehicles"; "get:vehicles"]
        let vehicles =
            [ for i in 0..9 do
                let vehicleId = createVehicleId()
                let vehicle =
                    CosmicDealership.Vehicle.V1.Vehicle(
                        Make="Falcon",
                        Model=sprintf "%i" i,
                        Year=2016)
                yield vehicleId, vehicle ]
        for vehicleId, vehicle in vehicles do
            addVehicle user vehicleId vehicle |> ignore
        Thread.Sleep 2000
        for (vehicleId, vehicle) in vehicles do
            let res = getVehicle user vehicleId
            match res.ResponseCase with
            | GetVehicleResponseCase.Success ->
                res.Success.VehicleId
                |> Expect.equal "should have same vehicle id" vehicleId
                res.Success.Vehicle
                |> Expect.equal "should be same vehicle" vehicle
                res.Success.Status.ToString()
                |> Expect.equal "should be available" "Available"
            | _ -> failwithf "error!: %A" res
    }

let testAddVehicleDenied =
    test "add vehicle denied" {
        let user = createUser []
        let vehicleId = createVehicleId()
        let vehicle =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Falcon",
                Model="9",
                Year=2016)
        let actualResponse = addVehicle user vehicleId vehicle
        let expectedResponse =
            CosmicDealership.Vehicle.V1.AddVehicleResponse(
                PermissionDenied="user test does not have add:vehicles permission")
        actualResponse
        |> Expect.equal "should have permission denied" expectedResponse
    }

let testUpdateVehicle =
    test "update vehicle" {
        let user = createUser ["add:vehicles"; "update:vehicles"; "get:vehicles"]
        let vehicleId = createVehicleId()
        let vehicle =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle user vehicleId vehicle |> ignore
        let newVehicle =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Hawk",
                Model="10",
                Year=2016)
        updateVehicle user vehicleId newVehicle |> ignore
        Thread.Sleep 2000
        let res = getVehicle user vehicleId
        match res.ResponseCase with
        | GetVehicleResponseCase.Success ->
            res.Success.VehicleId
            |> Expect.equal "should have same vehicle id" vehicleId
            res.Success.Vehicle
            |> Expect.equal "should equal updated vehicle" newVehicle
            res.Success.Status.ToString()
            |> Expect.equal "status should still be available" "Available"
        | _ -> failwithf "error!: %A" res
    }

let testRemoveVehicle =
    test "remove vehicle" {
        let user = createUser ["add:vehicles"; "remove:vehicles"; "get:vehicles"]
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle user vehicleId vehicle |> ignore
        removeVehicle user vehicleId |> ignore
        Thread.Sleep 2000
        let res = getVehicle user vehicleId
        match res.ResponseCase with
        | GetVehicleResponseCase.VehicleNotFound -> ()
        | _ -> failwithf "error!: %A" res
    }

let testRemoveVehicleDenied =
    test "remove vehicle denied" {
        let user = createUser ["add:vehicles"]
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle user vehicleId vehicle |> ignore
        let actualResponse = removeVehicle user vehicleId
        let expectedResponse =
            CosmicDealership.Vehicle.V1.RemoveVehicleResponse(
                PermissionDenied="user test does not have remove:vehicles permission")
        actualResponse
        |> Expect.equal "should have permission denied" expectedResponse
    }

let testListVehicles =
    test "list vehicles" {
        let user = createUser ["add:vehicles"; "list:vehicles"]
        let vehicleId1 = Guid.NewGuid().ToString("N")
        let vehicle1 =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Falcon",
                Model="99",
                Year=2016)
        addVehicle user vehicleId1 vehicle1 |> ignore
        let vehicleId2 = Guid.NewGuid().ToString("N")
        let vehicle2 =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Falcon",
                Model="100",
                Year=2016)
        addVehicle user vehicleId2 vehicle2 |> ignore
        Thread.Sleep 2000
        let actualVehicles = listVehicles user
        actualVehicles
        |> Seq.map (fun v -> v.VehicleId)
        |> Expect.sequenceContainsOrder "should contain vehicle id" [vehicleId1; vehicleId2]
    }

let testListAvailableVehicles =
    test "list available vehicles" {
        let user = createUser ["add:vehicles"]
        let vehicleId1 = Guid.NewGuid().ToString("N")
        let vehicle1 =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Falcon",
                Model="99",
                Year=2016)
        addVehicle user vehicleId1 vehicle1 |> ignore
        let vehicleId2 = Guid.NewGuid().ToString("N")
        let vehicle2 =
            CosmicDealership.Vehicle.V1.Vehicle(
                Make="Falcon",
                Model="100",
                Year=2016)
        addVehicle user vehicleId2 vehicle2 |> ignore
        Thread.Sleep 2000
        let actualVehicles = listAvailableVehicles ()
        actualVehicles
        |> Seq.map (fun v -> v.VehicleId)
        |> Expect.sequenceContainsOrder "should contain vehicle id" [vehicleId1; vehicleId2]
    }

[<Tests>]
let testVehicleService =
    testList "VehicleService" [
        testAddVehicle
        testAddVehicleDenied
        testRemoveVehicle
        testRemoveVehicleDenied
        testListVehicles
        testListAvailableVehicles
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
