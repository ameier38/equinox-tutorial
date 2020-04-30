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

let getVehicle (vehicleId:string) =
    let req = Tutorial.Vehicle.V1.GetVehicleRequest(VehicleId=vehicleId)
    let res = vehicleService.GetVehicle(req)
    res.Vehicle

let addVehicle (vehicleId:string) (vehicle:Tutorial.Vehicle.V1.Vehicle) =
    let req = Tutorial.Vehicle.V1.AddVehicleRequest(VehicleId=vehicleId, Vehicle=vehicle)
    vehicleService.AddVehicle(req)

let testAddVehicle =
    test "add vehicle" {
        let vehicleId = Guid.NewGuid().ToString()
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                Make="Ford",
                Model="Taurus",
                Year=1998)
        addVehicle vehicleId vehicle |> ignore
        let vehicleState = getVehicle vehicleId
        let expectedVehicleState =
            Tutorial.Vehicle.V1.VehicleState(
                Vehicle=vehicle,
                VehicleStatus=Tutorial.Vehicle.V1.VehicleStatus.Available)
        vehicleState
        |> Expect.equal "should return vehicle in available state" expectedVehicleState
    }

[<Tests>]
let testVehicleService =
    testList "test VehicleService" [
        testAddVehicle
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
