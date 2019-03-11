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
    | Undo of EventId
    | Create of Lease
    | SchedulePayment of ScheduledPayment
    | ReceivePayment of Payment
    | Terminate of EventEffectiveDate

type LeaseEvent =
    | Created of {| Lease: Lease; Context: EventContext |}
    | PaymentScheduled of {| ScheduledPayment: ScheduledPayment; Context: EventContext |}
    | PaymentReceived of {| Payment: Payment; Context: EventContext |}
    | Terminated of {| Context: EventContext |}

type StreamEvent = 
    | Undid of {| EventId: EventId; Context: EventContext |}
    | Reset of {| Context: EventContext |}

type Event =
    // stream events
    | Undid of {| EventId: EventId; Context: EventContext |}
    | Reset of {| Context: EventContext |}
    // domain events
    | Created of {| Lease: Lease; Context: EventContext |}
    | PaymentScheduled of {| ScheduledPayment: ScheduledPayment; Context: EventContext |}
    | PaymentReceived of {| Payment: Payment; Context: EventContext |}
    | Terminated of {| Context: EventContext |}
    interface TypeShape.UnionContract.IUnionContract

type StreamState =
    { DomainEvents: LeaseEvent list
      StreamEvents: StreamEvent list }

type LeaseStateData =
    { NextId: EventId
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
