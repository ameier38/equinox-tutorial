namespace Lease

type AsOfDate =
    | AsOn of AsOnDate
    | AsAt of AsAtDate

type EventContext =
    { EventEffectiveDate: EventEffectiveDate
      EventCreatedDate: EventCreatedDate }

type Lease =
    { LeaseId: LeaseId
      UserId: UserId
      StartDate: LeaseStartDate
      MaturityDate: LeaseMaturityDate
      MonthlyPaymentAmount: decimal<usd/month> }

type Payment =
    { PaymentId: PaymentId
      PaymentAmount: USD }

type LeaseCommand =
    | CreateLease of EventEffectiveDate * Lease
    | SchedulePayment of EventEffectiveDate * Payment
    | ReceivePayment of EventEffectiveDate * Payment
    | TerminateLease of EventEffectiveDate

type LeaseEvent =
    | LeaseCreated of {| Context: EventContext; Lease: Lease |}
    | PaymentScheduled of {| Context: EventContext; Payment: Payment |}
    | PaymentReceived of {| Context: EventContext; Payment: Payment |}
    | LeaseTerminated of {| Context: EventContext |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseEvents = LeaseEvent list

type LeaseObservation =
    { Lease: Lease
      TotalScheduled: USD
      TotalPaid: USD
      AmountDue: USD
      ScheduledPaymentIds: PaymentId list
      ReceivedPaymentIds: PaymentId list }

type LeaseState =
    | Nonexistent
    | Corrupt of string
    | Outstanding of LeaseObservation
    | Terminated of LeaseObservation
