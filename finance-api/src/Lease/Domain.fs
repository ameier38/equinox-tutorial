namespace Lease

open System

type AsOn =
    { AsAt: EventCreatedDate 
      AsOf: EventEffectiveDate }

type EventContext =
    { EventCreatedAt: EventCreatedDate
      EventEffectiveAt: EventEffectiveDate
      EventEffectiveOrder: EventEffectiveOrder
      EventType: EventType }

type AcceptedLease =
    { UserId: UserId
      VehicleId: VehicleId
      AcceptedAt: DateTimeOffset
      StartDate: DateTimeOffset 
      MaturityDate: DateTimeOffset
      MoneyFactor: decimal
      SalesTax: decimal
      VehicleValue: USD
      ResidualValue: decimal
      DownPayment: USD
      MonthlyPaymentAmount: decimal<usd/month>
      StartVehicleOdometer: Mile
      MilesPerYear: int<mile/year>
      MileOverageCharge: decimal<usd/mile> }

type RequestedPayment =
    { TransactionId: TransactionId 
      RequestedAt: DateTimeOffset 
      RequestedAmount: USD }

type RejectedPayment =
    { TransactionId: TransactionId
      RejectedAt: DateTimeOffset
      RejectedReason: string }

type SettledPayment =
    { TransactionId: TransactionId
      SettledAt: DateTimeOffset }

type ReturnedPayment =
    { TransactionId: TransactionId
      ReturnedAt: DateTimeOffset
      ReturnedReason: string }

type ReturnedVehicle =
    { VehicleId: VehicleId
      VehicleOdometer: Mile
      DamageCharge: USD }

type PaymentStatus =
    | Requested
    | Rejected
    | Settled
    | Received
    | Returned

type PaymentState =
    { PaymentStatus: PaymentStatus
      PaymentAmount: USD
      RequestedAt: DateTimeOffset
      RejectedAt: DateTimeOffset option
      SettledAt: DateTimeOffset option
      ReceivedAt: DateTimeOffset option
      ReturnedAt: DateTimeOffset option }

type LeaseCommand =
    | AcceptLease of AcceptedLease
    | RequestPayment of RequestedPayment
    | RejectPayment of RejectedPayment
    | SettlePayment of SettledPayment
    | ReturnPayment of ReturnedPayment
    | ReturnVehicle of ReturnedVehicle

type StoredLeaseEvent =
    | LeaseAccepted of {| EventContext: EventContext; AcceptedLease: AcceptedLease |}
    | PaymentRequested of {| EventContext: EventContext; RequestedPayment: RequestedPayment |}
    | PaymentRejected of {| EventContext: EventContext; RejectedPayment: RejectedPayment |}
    | PaymentSettled of {| EventContext: EventContext; SettledPayment: SettledPayment |}
    | PaymentReturned of {| EventContext: EventContext; ReturnedPayment: ReturnedPayment |}
    | VehicleReturned of {| EventContext: EventContext; ReturnedVehicle: ReturnedVehicle |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseEvent =
    | Stored of StoredLeaseEvent
    | DayEnded of DateTimeOffset

type LeaseState =
    { LeaseId: LeaseId
      EventContext: EventContext option
      AcceptedLease: AcceptedLease option
      ReturnedVehicle: ReturnedVehicle option
      Payments: Map<TransactionId,PaymentState>
      CumPaymentAmountScheduled: USD
      CumPaymentAmountReceived: USD
      CumPaymentAmountChargedOff: USD
      DaysPastDue: Day
      OutstandingPaymentAmount: USD
      UnpaidPaymentAmount: USD }

type LeaseStream = LeaseEvent list
