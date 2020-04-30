open Expecto
open Expecto.Flip
open Vehicle

let testEvolve =
    test "test evolve" {
        // GIVEN initial state
        let prevState = Aggregate.initial
        // WHEN vehicle is added
        let vehicle =
            { Make = "Ford"
              Model = "Taurus"
              Year = 1998 }
        let vehicleAdded = VehicleAdded vehicle
        let newState = Aggregate.evolve prevState vehicleAdded
        // THEN status should be Available
        let expectedState =
            { Vehicle = Some vehicle
              VehicleStatus = Available }
        newState
        |> Expect.equal "status should be Available" expectedState
        // GIVEN previous state
        let prevState = newState
        // WHEN vehicle is removed
        let vehicleRemoved = VehicleRemoved
        let newState = Aggregate.evolve prevState vehicleRemoved
        // THEN state should be Removed
        let expectedState =
            { prevState with
                VehicleStatus = Removed }
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
