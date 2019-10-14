module Lease.Types

open System

type AsOfInputDto =
    { AsAt: string option
      AsOn: string option }

type GetLeaseInputDto =
    { LeaseId: string
      AsOf: AsOfInputDto option }

type ListLeasesInputDto =
    { PageSize: int option
      PageToken: string option }

type ListLeaseEventsInputDto =
    { LeaseId: string
      AsOf: AsOfInputDto option
      PageSize: int option
      PageToken: string option }

type CreateLeaseInputDto =
    { LeaseId: string
      UserId: string
      CommencementDate: DateTime
      ExpirationDate: DateTime
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
