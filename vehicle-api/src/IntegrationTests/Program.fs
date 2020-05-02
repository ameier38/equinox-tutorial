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

let listVehicles () =
    let req = Tutorial.Vehicle.V1.ListVehiclesRequest(PageToken="", PageSize=100)
    let res = vehicleService.ListVehicles(req)
    res.Vehicles
    |> Seq.toList

let getVehicle (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.GetVehicleRequest(VehicleId=vehicleId)
    let res = vehicleService.GetVehicle(req)
    res.Vehicle

let addVehicle (vehicle:Tutorial.Vehicle.V1.Vehicle) =
    let req = Tutorial.Vehicle.V1.AddVehicleRequest(Vehicle=vehicle)
    vehicleService.AddVehicle(req)

let removeVehicle (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.RemoveVehicleRequest(VehicleId=vehicleId)
    vehicleService.RemoveVehicle(req)

let leaseVehicle (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.LeaseVehicleRequest(VehicleId=vehicleId)
    vehicleService.LeaseVehicle(req)

let returnVehicle (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.ReturnVehicleRequest(VehicleId=vehicleId)
    vehicleService.ReturnVehicle(req)

let testAddVehicle =
    test "add vehicle" {
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId=vehicleId,
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle vehicle |> ignore
        let vehicleState = getVehicle vehicleId
        let expectedVehicleState =
            Tutorial.Vehicle.V1.VehicleState(
                Vehicle=vehicle,
                VehicleStatus=Tutorial.Vehicle.V1.VehicleStatus.Available)
        vehicleState
        |> Expect.equal "should return vehicle in available state" expectedVehicleState
    }

let testRemoveVehicle =
    test "remove vehicle" {
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId=vehicleId,
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle vehicle |> ignore
        removeVehicle vehicleId |> ignore
        let vehicleState = getVehicle vehicleId
        let expectedVehicleState =
            Tutorial.Vehicle.V1.VehicleState(
                Vehicle=vehicle,
                VehicleStatus=Tutorial.Vehicle.V1.VehicleStatus.Removed)
        vehicleState
        |> Expect.equal "should return vehicle in removed state" expectedVehicleState
    }

let testListVehicles =
    testAsync "list vehicles" {
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
            addVehicle vehicle |> ignore
        do! Async.Sleep 2000
        let actualVehicles = listVehicles()  
        actualVehicles
        |> Expect.sequenceContainsOrder "should contain added vehicles" expectedVehicles
    }

[<Tests>]
let testVehicleService =
    testList "VehicleService" [
        testAddVehicle
        testRemoveVehicle
        testListVehicles
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
