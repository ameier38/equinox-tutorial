open Expecto
open Expecto.Flip
open System
open FSharp.Data.GraphQL

type LeaseProvider = GraphQLProvider<"http://localhost:4000">

let createLease = 
    LeaseProvider.Operation<"""
    mutation CreateLease($input: CreateLeaseInput!) {
        createLease(input: $input)
    }
    """>()

let getLease =
    LeaseProvider.Operation<"""
    query GetLease($input: GetLeaseInput!) {
        getLease(input: $input) {
            leaseId
            userId
            commencementDate
            expirationDate
            totalScheduled
            totalPaid
            amountDue
        }
    }
    """>()

let schedulePayment =
    LeaseProvider.Operation<"""
    mutation SchedulePayment($input: SchedulePaymentInput!) {
        schedulePayment(input: $input)
    }
    """>()

let receivePayment =
    LeaseProvider.Operation<"""
    mutation ReceivePayment($input: ReceivePaymentInput!) {
        receivePayment(input: $input)
    }
    """>()

let testLeaseWorkflow =
    testAsync "Can successfully run lease workflow" {
        let leaseId = Guid.NewGuid().ToString("N")
        let userId = Guid.NewGuid().ToString("N")
        let paymentId = Guid.NewGuid().ToString("N")
        let commencementDate = DateTime(2019, 1, 1)
        let expirationDate = commencementDate.AddMonths(12)
        let createLeaseInput = 
            LeaseProvider.Types.CreateLeaseInput(
                leaseId=leaseId,
                userId=userId,
                commencementDate=commencementDate,
                expirationDate=expirationDate,
                monthlyPaymentAmount=50.0)
        let! createLeaseResult = createLease.AsyncRun(createLeaseInput)
        createLeaseResult.Data
        |> Expect.isSome "should successfully create lease"
        let schedulePaymentInput =
            LeaseProvider.Types.SchedulePaymentInput(
                leaseId=leaseId,
                paymentId=paymentId,
                scheduledDate=DateTime(2019, 1, 2),
                scheduledAmount=50.0)
        let! schedulePaymentResult = schedulePayment.AsyncRun(schedulePaymentInput)
        schedulePaymentResult.Data
        |> Expect.isSome "should successfully schedule payment"
        let receivePaymentInput =
            LeaseProvider.Types.ReceivePaymentInput(
                leaseId=leaseId,
                paymentId=paymentId,
                receivedDate=DateTime(2019, 1, 3),
                receivedAmount=40.0) 
        let! receivePaymentResult = receivePayment.AsyncRun(receivePaymentInput)
        receivePaymentResult.Data
        |> Expect.isSome "should successfully receive payment"
        let getLeaseInput =
            LeaseProvider.Types.GetLeaseInput(
                leaseId=leaseId,
                asOf=LeaseProvider.Types.AsOf(asOn="2019-01-04"))
        let! getLeaseResult = getLease.AsyncRun(getLeaseInput)
        printfn "Data:\n%A" getLeaseResult.Data
        getLeaseResult.Data
        |> Option.map (fun res ->
            let obs = res.GetLease
            obs.LeaseId |> Expect.equal "should equal created leaseId" leaseId
            obs.CommencementDate |> Expect.equal "should equal commencementDate" commencementDate
            obs.ExpirationDate |> Expect.equal "should equal expirationDate" expirationDate
            obs.TotalScheduled |> Expect.equal "should equal zero" 50.0
            obs.TotalPaid |> Expect.equal "should equal zero" 40.0
            obs.AmountDue |> Expect.equal "should equal difference between scheduled and paid" 10.0
            )
        |> Expect.isSome "getLease should complete successfully"
    }

let tests =
    testList "All" [
        testLeaseWorkflow
    ]

[<EntryPoint>]
let main argv =
    runTestsWithArgs defaultConfig argv tests
