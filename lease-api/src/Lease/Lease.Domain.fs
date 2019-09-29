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
      StartDate: DateTime 
      MaturityDate: DateTime 
      MonthlyPaymentAmount: MonthlyPaymentAmount }

type LeaseStatus =
    | Outstanding
    | Terminated

type Payment =
    { PaymentId: PaymentId 
      PaymentDate: DateTime 
      PaymentAmount: USD }

type Termination =
    { TerminationId: TerminationId
      TerminationDate: DateTime
      TerminationReason: string }

type LeaseCommand =
    | CreateLease of Lease
    | SchedulePayment of Payment
    | ReceivePayment of Payment
    | TerminateLease of Termination

type Command =
    | LeaseCommand of LeaseCommand
    | DeleteEvent of EventId

type LeaseEvent =
    | LeaseCreated of EventContext * Lease
    | PaymentScheduled of EventContext * Payment
    | PaymentReceived of EventContext * Payment
    | LeaseTerminated of EventContext * Termination

type StoredEvent =
    | EventDeleted of {| EventContext: {| EventCreatedTime: EventCreatedTime |}; EventId: EventId |}
    | LeaseCreated of {| EventContext: EventContext; Lease: Lease |}
    | PaymentScheduled of {| EventContext: EventContext; Payment: Payment |}
    | PaymentReceived of {| EventContext: EventContext; Payment: Payment |}
    | LeaseTerminated of {| EventContext: EventContext; Termination: Termination |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseObservation =
    { Lease: Lease
      ObservationDate: DateTime
      CreatedTime: EventCreatedTime
      UpdatedTime: EventCreatedTime
      TotalScheduled: USD
      TotalPaid: USD
      AmountDue: USD
      LeaseStatus: LeaseStatus }

type LeaseState = LeaseObservation option

type LeaseStream =
    { NextEventId: EventId 
      LeaseEvents: LeaseEvent list 
      DeletedEvents: (EventCreatedTime * EventId) list }

type LeaseCreatedList = (EventContext * Lease) list

type LeaseEventList = LeaseEvent list
