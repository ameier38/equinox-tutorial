namespace Lease

open System

type EventContext =
    { EventId: EventId
      EventCreatedDate: EventCreatedDate
      EventEffectiveDate: EventEffectiveDate }

type Lease =
    { LeaseId: LeaseId
      StartDate: DateTime
      MaturityDate: DateTime
      MonthlyPaymentAmount: MonthlyPaymentAmount }

type ScheduledPayment =
    { ScheduledPaymentDate: DateTime
      ScheduledPaymentAmount: ScheduledPaymentAmount }

type Payment =
    { PaymentDate: DateTime
      PaymentAmount: PaymentAmount }

type LeaseCommand =
    | Create of Lease
    | SchedulePayment of ScheduledPayment
    | ReceivePayment of Payment
    | Terminate of EventEffectiveDate

type LeaseEvent =
    | Created of {| Lease: Lease; Context: EventContext |}
    | PaymentScheduled of {| ScheduledPayment: ScheduledPayment; Context: EventContext |}
    | PaymentReceived of {| Payment: Payment; Context: EventContext |}
    | Terminated of {| Context: EventContext |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseEvents = LeaseEvent list

type LeaseStateData =
    { NextId: EventId
      Events: (EventType * EventContext) list
      Lease: Lease
      TotalScheduled: ScheduledPaymentAmount
      TotalPaid: PaymentAmount
      AmountDue: decimal
      CreatedDate: DateTime
      UpdatedDate: DateTime }

type LeaseState =
    | NonExistent
    | Corrupt of string
    | Outstanding of LeaseStateData
    | Terminated of LeaseStateData
