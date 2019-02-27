namespace Lease

open System
open Equinox.UnionCodec
open Ouroboros

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

type LeaseEvent =
    | Undid of EventId
    | Compacted of LeaseEvent[]
    | Created of Lease * Context
    | Modified of Lease * Context
    | PaymentScheduled of Payment * Context
    | PaymentReceived of Payment * Context
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
