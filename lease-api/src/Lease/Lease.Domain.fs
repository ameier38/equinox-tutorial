namespace Lease

open System

type AsOfDate =
    { AsAt: EventCreatedTime
      AsOn: EventEffectiveDate }

type EventContext =
    { EventId: EventId
      EventCreatedTime: EventCreatedTime
      EventEffectiveDate: EventEffectiveDate }

type Lease =
    { LeaseId: LeaseId
      UserId: UserId
      StartDate: LeaseStartDate
      MaturityDate: LeaseMaturityDate
      MonthlyPaymentAmount: MonthlyPaymentAmount }

type LeaseStatus =
    | Outstanding
    | Terminated

type Payment =
    { PaymentId: PaymentId
      PaymentAmount: USD }

type LeaseCommand =
    | CreateLease of EventEffectiveDate * Lease
    | SchedulePayment of EventEffectiveDate * Payment
    | ReceivePayment of EventEffectiveDate * Payment
    | TerminateLease of EventEffectiveDate

type Command =
    | LeaseCommand of LeaseCommand
    | DeleteEvent of EventId

type LeaseEvent =
    | LeaseCreated of EventContext * Lease
    | PaymentScheduled of EventContext * Payment
    | PaymentReceived of EventContext * Payment
    | LeaseTerminated of EventContext

type StoredEvent =
    | EventDeleted of {| EventContext: {| EventCreatedDate: EventCreatedTime |}; EventId: EventId |}
    | LeaseCreated of {| EventContext: EventContext; Lease: Lease |}
    | PaymentScheduled of {| EventContext: EventContext; Payment: Payment |}
    | PaymentReceived of {| EventContext: EventContext; Payment: Payment |}
    | LeaseTerminated of {| EventContext: EventContext |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseObservation =
    { Lease: Lease
      TotalScheduled: USD
      TotalPaid: USD
      AmountDue: USD
      LeaseStatus: LeaseStatus }

type LeaseState = LeaseObservation option

type LeaseStream =
    { NextEventId: EventId
      LeaseEvents: LeaseEvent list
      DeletedEvents: (EventCreatedTime * EventId) list }

type LeaseList = (EventContext * Lease) list

type LeaseEventList = LeaseEvent list
