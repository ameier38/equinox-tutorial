open Expecto
open Expecto.Flip
open FSharp.ValidationBlocks
open Server
open Shared
open System

let testEvolve =
    test "test evolve" {
        // GIVEN initial state
        let prevState = Aggregate.initial
        // WHEN vehicle is added
        let vehicleId = VehicleId.create()
        let vehicle =
            { Make = Unchecked.blockof<Make> "Falcon"
              Model = Unchecked.blockof<Model> "9"
              Year = Unchecked.blockof<Year> 2016 }
        let vehicleAdded = VehicleAdded {| VehicleId = vehicleId; Vehicle = vehicle |}
        let newState = Aggregate.evolve prevState vehicleAdded
        // THEN status should be Available
        newState
        |> Expect.equal "status should be Available" Available
        // GIVEN previous state
        let prevState = newState
        // WHEN vehicle is updated
        let updatedVehicle = { vehicle with Model = Unchecked.blockof<Model> "10" }
        let vehicleUpdated = VehicleUpdated {| VehicleId = vehicleId; Vehicle = vehicle |}
        let newState = Aggregate.evolve prevState vehicleUpdated
        // THEN vehicle should be updated
        newState
        |> Expect.equal "vehicle should be updated" Available
        // GIVEN previous state
        let prevState = newState
        // WHEN vehicle is removed
        let vehicleRemoved = VehicleRemoved {| VehicleId = vehicleId |}
        let newState = Aggregate.evolve prevState vehicleRemoved
        // THEN state should be Removed
        newState
        |> Expect.equal "status should be Unknown" Unknown
    }

[<Tests>]
let testAggregate =
    testList "test Aggregate" [
        testEvolve
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
