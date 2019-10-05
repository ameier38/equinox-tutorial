module Lease.Client

open Shared.Operators
open Tutorial.Lease.V1
open System

module PageSize =
    let defaultValue (iOpt: int option) =
        iOpt
        |> Option.defaultValue 10

module PageToken =
    let defaultValue (sOpt: string option) =
        sOpt
        |> Option.defaultValue ""

module AsOfDateInputDto =
    let toProto (dtoOpt:Types.AsOfDateInputDto option) =
        let now = DateTime.UtcNow
        let defaultAsOfDate =
            AsOfDate(
                AsAtTime = !@@now,
                AsOnDate = !@now)
        dtoOpt
        |> Option.map (fun dto ->
            let asAt =
                dto.AsAt
                |> Option.map (DateTime.Parse >> Shared.DateTime.toProtoTimestamp)
                |> Option.defaultValue (now |> Shared.DateTime.toProtoTimestamp)
            let asOn =
                dto.AsOn
                |> Option.map (DateTime.Parse >> Shared.DateTime.toProtoDate)
                |> Option.defaultValue (now |> Shared.DateTime.toProtoDate)
            AsOfDate(
                AsAtTime = asAt,
                AsOnDate = asOn))
        |> Option.defaultValue defaultAsOfDate

module GetLeaseInputDto =
    let toProto (dto:Types.GetLeaseInputDto) =
        let asOfDate = 
            dto.AsOfDate
            |> AsOfDateInputDto.toProto
        GetLeaseRequest( 
            LeaseId = dto.LeaseId,
            AsOfDate = asOfDate)

module ListLeasesInputDto =
    let toProto (dto:Types.ListLeasesInputDto) =
        let asOfDate = 
            dto.AsOfDate
            |> AsOfDateInputDto.toProto
        let pageSize = dto.PageSize |> PageSize.defaultValue
        let pageToken = dto.PageToken |> PageToken.defaultValue
        ListLeasesRequest(
            AsOfDate = asOfDate,
            PageSize = pageSize,
            PageToken = pageToken)
        
module ListLeaseEventsInputDto =
    let toProto (dto:Types.ListLeaseEventsInputDto) =
        let asOfDate = 
            dto.AsOfDate
            |> AsOfDateInputDto.toProto
        let pageSize = dto.PageSize |> PageSize.defaultValue
        let pageToken = dto.PageToken |> PageToken.defaultValue
        ListLeaseEventsRequest(
            LeaseId = dto.LeaseId,
            AsOfDate = asOfDate,
            PageSize = pageSize,
            PageToken = pageToken)

module CreateLeaseInputDto =
    let toProto (dto:Types.CreateLeaseInputDto) =
        let lease =
            Lease(
                LeaseId = dto.LeaseId,
                UserId = dto.UserId,
                StartDate = !@dto.StartDate,
                MaturityDate = !@dto.MaturityDate,
                MonthlyPaymentAmount = !!dto.MonthlyPaymentAmount)
        CreateLeaseRequest(Lease = lease)

module SchedulePaymentInputDto =
    let toProto (dto:Types.SchedulePaymentInputDto) =
        let payment =
            ScheduledPayment(
                PaymentId = dto.PaymentId,
                ScheduledDate = !@dto.ScheduledDate,
                ScheduledAmount = !!dto.ScheduledAmount)
        SchedulePaymentRequest(
            LeaseId=dto.LeaseId, 
            ScheduledPayment=payment)

module ReceivePaymentInputDto =
    let toProto (dto:Types.ReceivePaymentInputDto) =
        let payment =
            ReceivedPayment(
                PaymentId = dto.PaymentId,
                ReceivedDate = !@dto.ReceivedDate,
                ReceivedAmount = !!dto.ReceivedAmount)
        ReceivePaymentRequest(
            LeaseId=dto.LeaseId, 
            ReceivedPayment=payment)

module TerminateLeaseInputDto =
    let toProto (dto:Types.TerminateLeaseInputDto) =
        let termination =
            Termination(
                TerminationDate = !@dto.TerminationDate,
                TerminationReason = dto.TerminationReason)
        TerminateLeaseRequest(Termination=termination)

module DeleteLeaseEventInputDto =
    let toProto (dto:Types.DeleteLeaseEventInputDto) =
        DeleteLeaseEventRequest(
            LeaseId = dto.LeaseId,
            EventId = dto.EventId)

type LeaseClient
    (   client:Tutorial.Lease.V1.LeaseAPI.LeaseAPIClient, 
        logger:Serilog.Core.Logger) =

    member __.GetLease(input:Types.GetLeaseInputDto) =
        logger.Information("GetLease {@GetLeaseInput}", input)
        let req = input |> GetLeaseInputDto.toProto
        client.GetLease(req).Lease

    member __.ListLeases(input:Types.ListLeasesInputDto) =
        logger.Information("ListLeases {@ListLeasesInput}", input)
        let req = input |> ListLeasesInputDto.toProto
        client.ListLeases(req)

    member __.ListLeaseEvents(input:Types.ListLeaseEventsInputDto) =
        logger.Information("ListLeaseEvents {@ListLeaseEventsInput}", input)
        let req = input |> ListLeaseEventsInputDto.toProto
        client.ListLeaseEvents(req)

    member __.CreateLease(input:Types.CreateLeaseInputDto) =
        logger.Information("CreateLease {@CreateLeaseInput}", input)
        let req = input |> CreateLeaseInputDto.toProto
        client.CreateLease(req).Message

    member __.SchedulePayment(input:Types.SchedulePaymentInputDto) =
        logger.Information("SchedulePayment {@SchedulePaymentInput}", input)
        let req = input |> SchedulePaymentInputDto.toProto
        client.SchedulePayment(req).Message

    member __.ReceivePayment(input:Types.ReceivePaymentInputDto) =
        logger.Information("ReceivePayment {@ReceivePaymentInput}", input)
        let req = input |> ReceivePaymentInputDto.toProto
        client.ReceivePayment(req).Message

    member __.TerminateLease(input:Types.TerminateLeaseInputDto) =
        logger.Information("TerminateLease {@TerminateLeaseInput}", input)
        let req = input |> TerminateLeaseInputDto.toProto
        client.TerminateLease(req).Message

    member __.DeleteLeaseEvent(input:Types.DeleteLeaseEventInputDto) =
        logger.Information("DeleteLeaseEvent {@DeleteLeaseEventInput}", input)
        let req = input |> DeleteLeaseEventInputDto.toProto
        client.DeleteLeaseEvent(req).Message
