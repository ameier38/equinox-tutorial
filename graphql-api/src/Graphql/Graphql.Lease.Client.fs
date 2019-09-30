module Graphql.Lease.Client

open Tutorial.Lease.V1
open Graphql.Operators

type LeaseClient
    (   client:Tutorial.Lease.V1.LeaseAPI.LeaseAPIClient, 
        logger:Serilog.Core.Logger) =

    member __.GetLease
        (   leaseId:string,
            asOfDate:AsOfDate) =
        let req = 
            GetLeaseRequest( 
                LeaseId = leaseId,
                AsOfDate = asOfDate)
        client.GetLease(req).Lease

    member __.ListLeases
        (   asOfDate:AsOfDate,
            pageSize:int,
            pageToken:string) =
        let req =
            ListLeasesRequest(
                AsOfDate = asOfDate,
                PageSize = pageSize,
                PageToken = pageToken)
        client.ListLeases(req)

    member __.ListLeaseEvents
        (   leaseId:string,
            asOfDate:AsOfDate,
            pageSize:int,
            pageToken:string) =
        let req =
            ListLeaseEventsRequest(
                LeaseId = leaseId,
                AsOfDate = asOfDate,
                PageSize = pageSize,
                PageToken = pageToken)
        client.ListLeaseEvents(req)

    member __.CreateLease
        (   input:Types.CreateLeaseInputDto) =
        let lease =
            Lease(
                LeaseId = input.LeaseId,
                UserId = input.UserId,
                StartDate = !@input.StartDate,
                MaturityDate = !@input.MaturityDate,
                MonthlyPaymentAmount = !!input.MonthlyPaymentAmount
            )
        let req = CreateLeaseRequest(Lease = lease)
        client.CreateLease(req).Message

    member __.SchedulePayment
        (   leaseId:string,
            paymentId:string,
            paymentDate:System.DateTime,
            paymentAmount:float) =
        try
            let payment =
                Tutorial.Lease.V1.Payment(
                    PaymentId = paymentId,
                    PaymentDate = (paymentDate |> DateTime.toProtoDate),
                    PaymentAmount = (paymentAmount |> decimal |> Money.fromDecimal))
            let req = 
                Tutorial.Lease.V1.SchedulePaymentRequest(
                    LeaseId=leaseId, 
                    Payment=payment)
            logger.Information(sprintf "scheduling payment:\n%A" req)
            let res = client.SchedulePayment(req)
            res.Message
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.AlreadyExists -> failwithf "lease-%s payment-%s already scheduled" leaseId paymentId
            | _ -> failwithf "Error:\n%A" ex

    member __.ReceivePayment
        (   leaseId:string,
            paymentId:string,
            paymentDate:System.DateTime,
            paymentAmount:float) =
        try
            let payment =
                Tutorial.Lease.V1.Payment(
                    PaymentId = paymentId,
                    PaymentDate = (paymentDate |> DateTime.toProtoDate),
                    PaymentAmount = (paymentAmount |> decimal |> Money.fromDecimal))
            let req = Tutorial.Lease.V1.ReceivePaymentRequest(LeaseId=leaseId, Payment=payment)
            logger.Information(sprintf "receive payment:\n%A" req)
            let res = client.ReceivePayment(req)
            res.Message
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.AlreadyExists -> failwithf "lease-%s payment-%s already received" leaseId paymentId
            | _ -> failwithf "Error:\n%A" ex

    member __.TerminateLease
        (   leaseId:string,
            effDate:System.DateTime) =
        let req = 
            Tutorial.Lease.V1.TerminateLeaseRequest(
                LeaseId=leaseId, 
                EffectiveDate=(effDate |> DateTime.toProtoDate))
        let res = client.TerminateLease(req)
        res.Message

    member __.DeleteEvent
        (   leaseId:string,
            eventId:int) =
        let req =
            Tutorial.Lease.V1.DeleteLeaseEventRequest(
                LeaseId = leaseId,
                EventId = eventId)
        let res = client.DeleteLeaseEvent(req)
        res.Message
