open Expecto
open Expecto.Flip
open Grpc.Core
open Shared
open System

let vehicleApiHost = Some "localhost" |> Env.getEnv "VEHICLE_API_HOST"
let vehicleApiPort = Some "50051" |> Env.getEnv "VEHICLE_API_PORT" |> int
let channelTarget = sprintf "%s:%d" vehicleApiHost vehicleApiPort

let vehicleChannel = Channel(channelTarget, ChannelCredentials.Insecure)
let vehicleService = Tutorial.Vehicle.V1.VehicleService.VehicleServiceClient(vehicleChannel)

let createUser (permissions:string list) =
    let user = Tutorial.User.V1.User(UserId="test")
    user.Permissions.AddRange(permissions)
    user

let assertPermissionDenied (permission:string) (ex:exn) =
    let msg = sprintf "user test does not have %s permission" permission
    match ex with
    | :? RpcException as ex ->
        ex.Status
        |> Expect.equal
            "should equal permission denied"
            (Status(StatusCode.PermissionDenied, msg))
    | other -> failwithf "wrong exception %A" other

let listVehicles (user:Tutorial.User.V1.User) =
    let req = Tutorial.Vehicle.V1.ListVehiclesRequest(User=user, PageToken="", PageSize=100)
    let res = vehicleService.ListVehicles(req)
    res.Vehicles
    |> Seq.toList

let getVehicle (user:Tutorial.User.V1.User) (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.GetVehicleRequest(User=user, VehicleId=vehicleId)
    let res = vehicleService.GetVehicle(req)
    res.Vehicle

let addVehicle (user:Tutorial.User.V1.User) (vehicle:Tutorial.Vehicle.V1.Vehicle) =
    let req = Tutorial.Vehicle.V1.AddVehicleRequest(User=user, Vehicle=vehicle)
    vehicleService.AddVehicle(req)

let removeVehicle (user:Tutorial.User.V1.User) (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.RemoveVehicleRequest(User=user, VehicleId=vehicleId)
    vehicleService.RemoveVehicle(req)

let leaseVehicle (user:Tutorial.User.V1.User) (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.LeaseVehicleRequest(User=user, VehicleId=vehicleId)
    vehicleService.LeaseVehicle(req)

let returnVehicle (user:Tutorial.User.V1.User) (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.ReturnVehicleRequest(User=user, VehicleId=vehicleId)
    vehicleService.ReturnVehicle(req)

let testAddVehicle =
    test "add vehicle" {
        let user = createUser ["add:vehicles"; "get:vehicles"]
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId=vehicleId,
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle user vehicle |> ignore
        let vehicleState = getVehicle user vehicleId
        let expectedVehicleState =
            Tutorial.Vehicle.V1.VehicleState(
                Vehicle=vehicle,
                VehicleStatus=Tutorial.Vehicle.V1.VehicleStatus.Available)
        vehicleState
        |> Expect.equal "should return vehicle in available state" expectedVehicleState
    }

let testAddVehicleDenied =
    test "add vehicle denied" {
        let user = createUser []
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId=vehicleId,
                Make="Falcon",
                Model="9",
                Year=2016)
        let f () = addVehicle user vehicle |> ignore
        let cont = assertPermissionDenied "add:vehicles"
        Expect.throwsC cont f
    }

let testRemoveVehicle =
    test "remove vehicle" {
        let user = createUser ["add:vehicles"; "get:vehicles"; "remove:vehicles"]
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId=vehicleId,
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle user vehicle |> ignore
        removeVehicle user vehicleId |> ignore
        let vehicleState = getVehicle user vehicleId
        let expectedVehicleState =
            Tutorial.Vehicle.V1.VehicleState(
                Vehicle=vehicle,
                VehicleStatus=Tutorial.Vehicle.V1.VehicleStatus.Removed)
        vehicleState
        |> Expect.equal "should return vehicle in removed state" expectedVehicleState
    }

let testRemoveVehicleDenied =
    test "remove vehicle denied" {
        let user = createUser ["add:vehicles"]
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId=vehicleId,
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle user vehicle |> ignore
        let f () = removeVehicle user vehicleId |> ignore
        let cont = assertPermissionDenied "remove:vehicles"
        Expect.throwsC cont f
    }

let testListVehicles =
    testAsync "list vehicles" {
        let user = createUser ["add:vehicles"; "list:vehicles"]
        let expectedVehicles =
            [ for i in 1..5 do 
                let vehicleId = Guid.NewGuid().ToString("N")
                yield
                    Tutorial.Vehicle.V1.Vehicle(
                        VehicleId=vehicleId,
                        Make="Falcon",
                        Model=(sprintf "%i" i),
                        Year=2014 + i)
            ]
        for vehicle in expectedVehicles do
            addVehicle user vehicle |> ignore
        do! Async.Sleep 2000
        let actualVehicles = listVehicles user
        actualVehicles
        |> Expect.sequenceContainsOrder "should contain added vehicles" expectedVehicles
    }

let testListVehiclesDenied =
    testAsync "list vehicles denied" {
        let user = createUser ["add:vehicles"]
        let expectedVehicles =
            [ for i in 1..5 do 
                let vehicleId = Guid.NewGuid().ToString("N")
                yield
                    Tutorial.Vehicle.V1.Vehicle(
                        VehicleId=vehicleId,
                        Make="Falcon",
                        Model=(sprintf "%i" i),
                        Year=2014 + i)
            ]
        for vehicle in expectedVehicles do
            addVehicle user vehicle |> ignore
        do! Async.Sleep 2000
        let f () = listVehicles user |> ignore
        let cont = assertPermissionDenied "list:vehicles"
        Expect.throwsC cont f
    }

[<Tests>]
let testVehicleService =
    testList "VehicleService" [
        testAddVehicle
        testAddVehicleDenied
        testRemoveVehicle
        testRemoveVehicleDenied
        testListVehicles
        testListVehiclesDenied
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
