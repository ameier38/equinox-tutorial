namespace Lease

open System
open Ouroboros

type Lease =
    { LeaseId: LeaseId
      StartDate: DateTime
      MaturityDate: DateTime
      MonthlyPaymentAmount: decimal }

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

type LeaseCommand =
    | Undo of EventId
    | Create of Lease
    | Modify of Lease * EffectiveDate
    | SchedulePayment of decimal * EffectiveDate
    | ReceivePayment of decimal * EffectiveDate 
    | Terminate of EffectiveDate

type LeaseEvent =
    | Undid of EventId
    | Created of Lease * Context
    | Modified of Lease * Context
    | PaymentScheduled of decimal * Context
    | PaymentReceived of decimal * Context
    | Terminated of Context
    interface TypeShape.UnionContract.IUnionContract
