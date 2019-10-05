module Graphql.Lease.Types

open System

type AsOfDateInputDto =
    { AsAt: string option
      AsOn: string option }

type GetLeaseInputDto =
    { LeaseId: string
      AsOfDate: AsOfDateInputDto option }

type ListLeasesInputDto =
    { AsOfDate: AsOfDateInputDto option
      PageSize: int option
      PageToken: string option }

type ListLeaseEventsInputDto =
    { LeaseId: string
      AsOfDate: AsOfDateInputDto option
      PageSize: int option
      PageToken: string option }

type CreateLeaseInputDto =
    { LeaseId: string
      UserId: string
      StartDate: DateTime
      MaturityDate: DateTime
      MonthlyPaymentAmount: float }

type SchedulePaymentInputDto = 
    { LeaseId: string
      PaymentId: string
      ScheduledDate: DateTime
      ScheduledAmount: float }

type ReceivePaymentInputDto =
    { LeaseId: string
      PaymentId: string
      ReceivedDate: DateTime
      ReceivedAmount: float }

type TerminateLeaseInputDto =
    { LeaseId: string
      TerminationDate: DateTime
      TerminationReason: string }

type DeleteLeaseEventInputDto =
    { LeaseId: string
      EventId: int }
