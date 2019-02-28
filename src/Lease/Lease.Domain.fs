namespace Lease

open System

type NewLease =
    { StartDate: DateTime
      MaturityDate: DateTime
      MonthlyPaymentAmount: decimal }

type Lease =
    { LeaseId: LeaseId
      StartDate: DateTime
      MaturityDate: DateTime
      MonthlyPaymentAmount: decimal }

type Payment =
    { PaymentDate: DateTime
      PaymentAmount: decimal }

type LeaseCommand =
    | Undo of EventId
    | Create of Lease
    | Modify of Lease * EffectiveDate
    | SchedulePayment of Payment
    | ReceivePayment of Payment
    | Terminate of EffectiveDate

type LeaseInfo =
    { Lease: Lease
      Context: Context }

type PaymentInfo =
    { Payment: Payment
      Context: Context }

type LeaseEvent =
    | Undid of EventId
    | Compacted of LeaseEvent[]
    | Created of LeaseInfo
    | Modified of LeaseInfo
    | PaymentScheduled of PaymentInfo
    | PaymentReceived of PaymentInfo
    | Terminated of Context
    interface TypeShape.UnionContract.IUnionContract

type LeaseStateData =
    { Lease: Lease
      TotalScheduled: decimal
      TotalPaid: decimal
      AmountDue: decimal }

type LeaseState =
    | NonExistent
    | Corrupt of string
    | Outstanding of LeaseStateData
    | Terminated of LeaseStateData
