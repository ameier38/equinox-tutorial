open Expecto
open Expecto.Flip
open FSharp.UMX
open Server
open Shared

let emptyState =
    { VehicleStatus = Unknown
      AvatarUrl = UMX.tag<url> ""
      ImageUrls = [] }

let testEvolve =
    test "test evolve" {
        // GIVEN initial state
        let prevState = Aggregate.initial
        // WHEN vehicle is added
        let vehicleId = VehicleId.create()
        let vehicle =
            { Make = UMX.tag<make> "Falcon"
              Model = UMX.tag<model> "9"
              Year = UMX.tag<year> 2016 }
        let addVehicle = AddVehicle vehicle
        let events = Aggregate.decide vehicleId addVehicle prevState |> snd
        let newState = events |> List.fold Aggregate.evolve prevState
        // THEN status should be Available
        let expectedState = { emptyState with VehicleStatus = Available }
        newState
        |> Expect.equal "status should be Available" expectedState
        // GIVEN previous state
        let prevState = newState
        // WHEN vehicle is updated
        let updatedVehicle = { vehicle with Model = UMX.tag<model> "10" }
        let updateVehicle = UpdateVehicle updatedVehicle
        let events = Aggregate.decide vehicleId updateVehicle prevState |> snd
        let newState = events |> List.fold Aggregate.evolve prevState
        // THEN vehicle should be updated
        newState
        |> Expect.equal "status should not change" prevState
        // GIVEN previous state
        let prevState = newState
        // WHEN vehicle is removed
        let removeVehicle = RemoveVehicle
        let events = Aggregate.decide vehicleId removeVehicle prevState |> snd
        let newState = events |> List.fold Aggregate.evolve prevState
        // THEN state should be Removed
        let expectedState = { prevState with VehicleStatus = Unknown }
        newState
        |> Expect.equal "status should be Unknown" expectedState
    }

[<Tests>]
let testAggregate =
    testList "test Aggregate" [
        testEvolve
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
