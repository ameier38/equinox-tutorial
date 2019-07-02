module Graphql.Lease.Client

open Graphql
open Grpc.Core

type AsOfDateInput =
    { 
        AsAt: string option 
        AsOn: string option 
    } 
type AsOfDate =
    { 
        AsAt: System.DateTime 
        AsOn: System.DateTime 
    }
module AsOfDate =
    let getDefault () =
        let now = System.DateTime.UtcNow
        { AsAt = now
          AsOn = now }
    let fromInput (input:AsOfDateInput) =
        let now = System.DateTime.UtcNow
        { AsOfDate.AsAt =
            input.AsAt
            |> Option.map System.DateTime.Parse
            |> Option.defaultValue now
          AsOfDate.AsOn =
            input.AsOn
            |> Option.map System.DateTime.Parse
            |> Option.defaultValue now }
    let toProto (asOfDate:AsOfDate) =
        Tutorial.Lease.V1.AsOfDate(
            AsAtTime = (asOfDate.AsAt |> DateTime.toProtoTimestamp),
            AsOnDate = (asOfDate.AsOn |> DateTime.toProtoDate))

type LeaseStatus =
    | Outstanding
    | Terminated
module LeaseStatus =
    let fromProto (proto:Tutorial.Lease.V1.LeaseStatus) =
        match proto with
        | Tutorial.Lease.V1.LeaseStatus.Outstanding -> Outstanding
        | Tutorial.Lease.V1.LeaseStatus.Terminated -> Terminated
        | other -> failwithf "invalid LeaseStatus %A" other

type Payment =
    {
        PaymentId: string
        PaymentDate: System.DateTime
        PaymentAmount: decimal
    }

type LeaseEvent =
    {
        EventId: int
        EventCreatedTime: System.DateTime
        EventEffectiveDate: System.DateTime
        EventType: string
    }
module LeaseEvent =
    let fromProto (proto:Tutorial.Lease.V1.LeaseEvent) =
        {
            EventId = proto.EventId
            EventCreatedTime = proto.EventCreatedTime.ToDateTime()
            EventEffectiveDate = proto.EventEffectiveDate.ToDateTime()
            EventType = proto.EventType
        }

type ListLeaseEventsResponse =
    { 
        Events: LeaseEvent list
        PrevPageToken: string
        NextPageToken: string
        TotalCount: int
    }
module ListLeaseEventsResponse =
    let fromProto (proto:Tutorial.Lease.V1.ListLeaseEventsResponse) =
        { Events = proto.Events |> Seq.map LeaseEvent.fromProto |> Seq.toList
          PrevPageToken = proto.PrevPageToken
          NextPageToken = proto.NextPageToken
          TotalCount = proto.TotalCount }

type Lease =
    { 
        LeaseId: string
        UserId: string
        StartDate: System.DateTime 
        MaturityDate: System.DateTime 
        MonthlyPaymentAmount: decimal
    }
module Lease =
    let fromProto (proto:Tutorial.Lease.V1.Lease) =
        { 
            LeaseId = proto.LeaseId
            UserId = proto.UserId
            StartDate = proto.StartDate.ToDateTime() 
            MaturityDate = proto.MaturityDate.ToDateTime() 
            MonthlyPaymentAmount = proto.MonthlyPaymentAmount.DecimalValue
        }

type ListLeasesResponse =
    {
        Leases: Lease list
        PrevPageToken: string
        NextPageToken: string
        TotalCount: int
    }
module ListLeasesResponse =
    let fromProto (proto:Tutorial.Lease.V1.ListLeasesResponse) =
        {
            Leases = proto.Leases |> Seq.map Lease.fromProto |> Seq.toList
            PrevPageToken = proto.PrevPageToken
            NextPageToken = proto.NextPageToken
            TotalCount = proto.TotalCount
        }

type LeaseObservation =
    {
        AsOfDate: AsOfDate
        Lease: Lease
        TotalScheduled: decimal
        TotalPaid: decimal
        AmountDue: decimal
        LeaseStatus: LeaseStatus 
    }
module LeaseObservation =
    let fromProto (asOfDate:AsOfDate) (proto:Tutorial.Lease.V1.LeaseObservation) =
        {
            AsOfDate = asOfDate
            Lease = proto.Lease |> Lease.fromProto
            TotalScheduled = proto.TotalScheduled.DecimalValue
            TotalPaid = proto.TotalPaid.DecimalValue
            AmountDue = proto.AmountDue.DecimalValue
            LeaseStatus = proto.LeaseStatus |> LeaseStatus.fromProto
        }

type LeaseClient
    (   client:Tutorial.Lease.V1.LeaseAPI.LeaseAPIClient, 
        logger:Serilog.Core.Logger) =

    member __.GetLease
        (   leaseId:string,
            asOfDate:AsOfDate) =
        try
            let req = 
                Tutorial.Lease.V1.GetLeaseRequest( 
                    LeaseId = leaseId,
                    AsOfDate = (asOfDate |> AsOfDate.toProto))
            let res = client.GetLease(req)
            res.Lease |> LeaseObservation.fromProto asOfDate
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.NotFound -> failwithf "lease-%s does not exist as of %A" leaseId asOfDate
            | _ -> failwithf "Error!:\n%A" ex

    member __.ListLeases
        (   asOfDate:AsOfDate,
            pageSize:int,
            pageToken:string) =
        let req =
            Tutorial.Lease.V1.ListLeasesRequest(
                AsOfDate = (asOfDate |> AsOfDate.toProto),
                PageSize = pageSize,
                PageToken = pageToken)
        client.ListLeases(req)
        |> ListLeasesResponse.fromProto

    member __.ListLeaseEvents
        (   leaseId:string,
            asOfDate:AsOfDate,
            pageSize:int,
            pageToken:string) =
        let req =
            Tutorial.Lease.V1.ListLeaseEventsRequest(
                LeaseId = leaseId,
                AsOfDate = (asOfDate |> AsOfDate.toProto),
                PageSize = pageSize,
                PageToken = pageToken)
        client.ListLeaseEvents(req)
        |> ListLeaseEventsResponse.fromProto

    member __.CreateLease
        (   leaseId:string,
            userId:string, 
            startDate:System.DateTime, 
            maturityDate:System.DateTime, 
            monthlyPaymentAmount:float) =
        try
            let lease =
                Tutorial.Lease.V1.Lease(
                    LeaseId = leaseId,
                    UserId = userId,
                    StartDate = (startDate |> DateTime.toProtoDate),
                    MaturityDate = (maturityDate |> DateTime.toProtoDate),
                    MonthlyPaymentAmount = (monthlyPaymentAmount |> decimal |> Money.fromDecimal)
                )
            let req = Tutorial.Lease.V1.CreateLeaseRequest(Lease = lease)
            let res = client.CreateLease(req)
            res.Message
        with
        | :? RpcException as ex ->
            match ex.StatusCode with
            | StatusCode.AlreadyExists -> failwithf "lease-%s already exists" leaseId
            | _ -> failwithf "Error!:\n%A" ex

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
