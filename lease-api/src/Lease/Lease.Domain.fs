namespace Lease

open System

type AsOf =
    { AsAt: EventCreatedTime 
      AsOn: EventEffectiveDate }

type EventContext =
    { EventId: EventId 
      EventCreatedTime: EventCreatedTime 
      EventEffectiveDate: EventEffectiveDate }

type Lease =
    { LeaseId: LeaseId 
      UserId: UserId 
      CommencementDate: DateTime 
      ExpirationDate: DateTime
      MonthlyPaymentAmount: MonthlyPaymentAmount }

type ScheduledPayment =
    { PaymentId: PaymentId 
      ScheduledDate: DateTime 
      ScheduledAmount: USD }

type ReceivedPayment =
    { PaymentId: PaymentId
      ReceivedDate: DateTime
      ReceivedAmount: USD }

type Termination =
    { TerminationDate: DateTime
      TerminationReason: string }

type LeaseCommand =
    | CreateLease of Lease
    | SchedulePayment of ScheduledPayment
    | ReceivePayment of ReceivedPayment
    | TerminateLease of Termination

type Command =
    | LeaseCommand of LeaseCommand
    | DeleteEvent of EventId

type LeaseEvent =
    | LeaseCreated of EventContext * Lease
    | PaymentScheduled of EventContext * ScheduledPayment
    | PaymentReceived of EventContext * ReceivedPayment
    | LeaseTerminated of EventContext * Termination

type StoredEvent =
    | EventDeleted of {| EventContext: {| EventCreatedTime: EventCreatedTime |}; EventId: EventId |}
    | LeaseCreated of {| EventContext: EventContext; Lease: Lease |}
    | PaymentScheduled of {| EventContext: EventContext; ScheduledPayment: ScheduledPayment |}
    | PaymentReceived of {| EventContext: EventContext; ReceivedPayment: ReceivedPayment |}
    | LeaseTerminated of {| EventContext: EventContext; Termination: Termination |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseStatus =
    | Outstanding
    | Terminated

type LeaseObservation =
    { CreatedAt: EventCreatedTime
      UpdatedAt: EventCreatedTime
      UpdatedOn: EventEffectiveDate
      LeaseId: LeaseId
      UserId: UserId
      CommencementDate: DateTime
      ExpirationDate: DateTime
      MonthlyPaymentAmount: MonthlyPaymentAmount
      TotalScheduled: USD
      TotalPaid: USD
      AmountDue: USD
      LeaseStatus: LeaseStatus
      TerminatedDate: DateTime option }

type LeaseState = LeaseObservation option

type LeaseStream =
    { NextEventId: EventId 
      LeaseEvents: LeaseEvent list 
      DeletedEvents: (EventCreatedTime * EventId) list }

type LeaseCreatedList = (EventContext * Lease) list

type LeaseEventList = LeaseEvent list
