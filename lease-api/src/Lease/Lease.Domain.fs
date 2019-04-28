namespace Lease

type AsOfDate =
    { AsAt: EventCreatedDate
      AsOn: EventEffectiveDate }

type LeaseEventContext =
    { EventId: EventId
      EventCreatedDate: EventCreatedDate 
      EventEffectiveDate: EventEffectiveDate }

type NewLease =
    { LeaseId: LeaseId
      UserId: UserId
      MaturityDate: LeaseMaturityDate
      MonthlyPaymentAmount: MonthlyPaymentAmount }

type LeaseStatus =
    | Outstanding
    | Terminated

type LeaseCommand =
    | CreateLease of EventEffectiveDate * NewLease
    | SchedulePayment of EventEffectiveDate * USD
    | ReceivePayment of EventEffectiveDate * USD
    | TerminateLease of EventEffectiveDate

type Command =
    | LeaseCommand of LeaseCommand
    | DeleteEvent of EventId

type LeaseEvent =
    | LeaseCreated of LeaseEventContext * NewLease
    | PaymentScheduled of LeaseEventContext * USD
    | PaymentReceived of LeaseEventContext * USD
    | LeaseTerminated of LeaseEventContext

type StoredEvent =
    | EventDeleted of {| EventContext: {| EventCreatedDate: EventCreatedDate |}; EventId: EventId |}
    | LeaseCreated of {| EventContext: LeaseEventContext; NewLease: NewLease |}
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

type LeaseState = Result<LeaseObservation option, string>

type StreamState =
    { NextEventId: EventId
      LeaseEvents: LeaseEvent list
      DeletedEvents: (EventCreatedDate * EventId) list }
