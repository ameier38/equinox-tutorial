open Expecto
open Expecto.Flip
open Grpc.Core
open MongoDB.Driver
open Shared
open System
open System.Threading

let vehicleApiHost = Env.getEnv "VEHICLE_API_HOST" "localhost"
let vehicleApiPort = Env.getEnv "VEHICLE_API_PORT" "50051" |> int
let channelTarget = sprintf "%s:%d" vehicleApiHost vehicleApiPort

let vehicleChannel = Channel(channelTarget, ChannelCredentials.Insecure)
let vehicleService = Tutorial.Vehicle.V1.VehicleService.VehicleServiceClient(vehicleChannel)

let mongoHost = Env.getEnv "MONGO_HOST" "localhost"
let mongoPort = Env.getEnv "MONGO_PORT" "27017" |> int
let mongoUser = Env.getEnv "MONGO_USER" "admin"
let mongoPassword = Env.getEnv "MONGO_PASSWORD" "changeit"
let mongoUrl = sprintf "mongodb://%s:%s@%s:%d" mongoUser mongoPassword mongoHost mongoPort

type VehicleDto =
    { vehicleId: string
      status: string }

type Store() =
    let vehicleCollectionName = "vehicles"
    let mongo = MongoClient(mongoUrl)
    let db = mongo.GetDatabase("dealership")
    let vehicleCollection = db.GetCollection<VehicleDto>(vehicleCollectionName)

    member _.GetVehicle(vehicleId:string) =
        vehicleCollection
            .Find(fun doc -> doc.vehicleId = vehicleId)
            .Project(fun doc -> { vehicleId = doc.vehicleId; status = doc.status })
            .First()

let store = Store()

let createVehicleId () = Guid.NewGuid().ToString("N")

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
        let vehicles =
            [ for i in 1..10 do
                let vehicleId = createVehicleId()
                let vehicle =
                    Tutorial.Vehicle.V1.Vehicle(
                        VehicleId=vehicleId,
                        Make="Falcon",
                        Model=sprintf "%i" i,
                        Year=2016)
                addVehicle user vehicle |> ignore
                yield vehicleId ]
        Thread.Sleep 2000
        for vehicleId in vehicles do
            let vehicleState = store.GetVehicle(vehicleId)
            let expectedVehicleState =
                { vehicleId = vehicleId
                  status = "Available" }
            vehicleState
            |> Expect.equal "should return vehicle in available state" expectedVehicleState
    }

let testAddVehicleDenied =
    test "add vehicle denied" {
        let user = createUser []
        let vehicleId = createVehicleId()
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
        let user = createUser ["add:vehicles"; "remove:vehicles"]
        let vehicleId = Guid.NewGuid().ToString("N")
        let vehicle =
            Tutorial.Vehicle.V1.Vehicle(
                VehicleId=vehicleId,
                Make="Falcon",
                Model="9",
                Year=2016)
        addVehicle user vehicle |> ignore
        removeVehicle user vehicleId |> ignore
        Thread.Sleep 2000
        let vehicleState = store.GetVehicle(vehicleId)
        let expectedVehicleState =
            { vehicleId = vehicleId
              status = "Removed" }
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

[<Tests>]
let testVehicleService =
    testList "VehicleService" [
        testAddVehicle
        testAddVehicleDenied
        testRemoveVehicle
        testRemoveVehicleDenied
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
