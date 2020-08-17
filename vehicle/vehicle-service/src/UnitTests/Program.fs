open Expecto
open Expecto.Flip
open Server
open Shared

let testEvolve =
    test "test evolve" {
        // GIVEN initial state
        let prevState = Aggregate.initial
        // WHEN vehicle is added
        let vehicle =
            { VehicleId = VehicleId.create()
              Make = "Falcon"
              Model = "9"
              Year = 2016 }
        let vehicleAdded = VehicleAdded vehicle
        let newState = Aggregate.evolve prevState vehicleAdded
        // THEN status should be Available
        let expectedState = Available vehicle
        newState
        |> Expect.equal "status should be Available" expectedState
        // GIVEN previous state
        let prevState = newState
        // WHEN vehicle is updated
        let updatedVehicle = { vehicle with Model = "10" }
        let vehicleUpdated = VehicleUpdated updatedVehicle
        let newState = Aggregate.evolve prevState vehicleUpdated
        // THEN vehicle should be updated
        let expectedState = Available updatedVehicle
        newState
        |> Expect.equal "vehicle should be updated" expectedState
        // GIVEN previous state
        let prevState = newState
        // WHEN vehicle is removed
        let vehicleRemoved = VehicleRemoved {| VehicleId = vehicle.VehicleId |}
        let newState = Aggregate.evolve prevState vehicleRemoved
        // THEN state should be Removed
        let expectedState = Removed
        newState
        |> Expect.equal "status should be Removed" expectedState
    }

[<Tests>]
let testAggregate =
    testList "test Aggregate" [
        testEvolve
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
