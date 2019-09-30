module Graphql.Lease.Types

open System

type AsOfDateInputDto =
    { AsAt: string option
      AsOn: string option }

type GetLeaseInputDto =
    { LeaseId: string
      AsOfDate: AsOfDateInputDto option }

type CreateLeaseInputDto =
    { LeaseId: string
      UserId: string
      StartDate: DateTime
      MaturityDate: DateTime
      MonthlyPaymentAmount: float }

type ScheduledPaymentInputDto = 
    { PaymentId: string
      ScheduledDate: DateTime
      ScheduledAmount: float }

type ReceivedPaymentInputDto =
    { PaymentId: string
      ReceivedDate: DateTime
      ReceivedAmount: float }
