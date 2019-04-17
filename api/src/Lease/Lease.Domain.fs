namespace Lease

type AsOfDate = AsOfDate of EventEffectiveDate * EventCreatedDate

type LeaseEventContext =
    { EventId: EventId
      EventCreatedDate: EventCreatedDate 
      EventEffectiveDate: EventEffectiveDate }

type NewLease =
    { LeaseId: LeaseId
      UserId: UserId
      MaturityDate: LeaseMaturityDate
      MonthlyPaymentAmount: MonthlyPaymentAmount }

type UpdatedLease =
    { LeaseId: LeaseId
      MaturityDate: LeaseMaturityDate option
      MonthlyPaymentAmount: MonthlyPaymentAmount option }

type LeaseStatus =
    | Outstanding
    | Terminated

type LeaseCommand =
    | CreateLease of EventEffectiveDate * NewLease
    | UpdateLease of EventEffectiveDate * UpdatedLease
    | SchedulePayment of EventEffectiveDate * USD
    | ReceivePayment of EventEffectiveDate * USD
    | TerminateLease of EventEffectiveDate

type LeaseEvent =
    | LeaseCreated of LeaseEventContext * NewLease
    | LeaseUpdated of LeaseEventContext * UpdatedLease
    | PaymentScheduled of LeaseEventContext * USD
    | PaymentReceived of LeaseEventContext * USD
    | LeaseTerminated of LeaseEventContext

type StoredEvent =
    | EventDeleted of {| EventContext: {| EventCreatedDate: EventCreatedDate |}; EventId: EventId |}
    | LeaseCreated of {| EventContext: LeaseEventContext; NewLease: NewLease |}
    | LeaseUpdated of {| EventContext: LeaseEventContext; UpdatedLease: UpdatedLease |}
    | PaymentScheduled of {| EventContext: LeaseEventContext; ScheduledAmount: USD |}
    | PaymentReceived of {| EventContext: LeaseEventContext; ReceivedAmount: USD |}
    | LeaseTerminated of {| EventContext: LeaseEventContext |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseObservation =
    { LeaseId: LeaseId
      UserId: UserId
      StartDate: LeaseStartDate
      MaturityDate: LeaseMaturityDate
      MonthlyPaymentAmount: MonthlyPaymentAmount
      TotalScheduled: USD
      TotalPaid: USD
      AmountDue: USD
      LeaseStatus: LeaseStatus }

type LeaseState =
    | Nonexistent
    | Corrupt of string
    | Exists of LeaseObservation

type StreamState =
    { NextEventId: EventId
      LeaseEvents: LeaseEvent list
      DeletedEvents: (EventCreatedDate * EventId) list }
