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

type LeaseTermination =
    { TerminationId: TerminationId
      TerminationDate: DateTime
      TerminationReason: string }

type LeaseCommand =
    | CreateLease of Lease
    | SchedulePayment of Payment
    | ReceivePayment of Payment
    | TerminateLease of DateTime

type Command =
    | LeaseCommand of LeaseCommand
    | DeleteEvent of EventId

type LeaseEvent =
    | LeaseCreated of EventContext * Lease
    | PaymentScheduled of EventContext * Payment
    | PaymentReceived of EventContext * Payment
    | LeaseTerminated of EventContext * LeaseTermination

type StoredEvent =
    | EventDeleted of {| EventContext: {| EventCreatedTime: EventCreatedTime |}; EventId: EventId |}
    | LeaseCreated of {| EventContext: EventContext; Lease: Lease |}
    | PaymentScheduled of {| EventContext: EventContext; Payment: Payment |}
    | PaymentReceived of {| EventContext: EventContext; Payment: Payment |}
    | LeaseTerminated of {| EventContext: EventContext; LeaseTermination: LeaseTermination |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseObservation =
    { Lease: Lease 
      CreatedTime: DateTime
      UpdatedTime: DateTime
      TotalScheduled: USD 
      TotalPaid: USD 
      AmountDue: USD 
      LeaseStatus: LeaseStatus }

type LeaseState = LeaseObservation option

type LeaseStream =
    { NextEventId: EventId 
      LeaseEvents: LeaseEvent list 
      DeletedEvents: (EventCreatedTime * EventId) list }

type LeaseCreatedList = Lease list

type LeaseEventList = LeaseEvent list
