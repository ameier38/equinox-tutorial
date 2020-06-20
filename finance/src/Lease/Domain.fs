namespace Lease

open System

type AsOn =
    { AsAt: EventCreatedAt 
      AsOf: EventEffectiveAt }

type EventContext =
    { EventCreatedAt: EventCreatedAt
      EventEffectiveAt: EventEffectiveAt }

type Observation =
    { ObservedAt: EventEffectiveAt
      ObservationOrder: EventEffectiveOrder
      ObservationDescription: EventType }

type AcceptedLease =
    { UserId: UserId
      VehicleId: VehicleId
      AcceptedAt: DateTimeOffset
      StartDate: DateTimeOffset 
      MaturityDate: DateTimeOffset
      MoneyFactor: decimal
      SalesTaxRate: decimal
      VehicleValue: USD
      VehicleResidualValue: decimal
      DownPayment: USD
      MonthlyPayment: decimal<usd/month>
      VehicleOdometer: Mile
      MilesPerYear: int<mile/year>
      MileOverageCharge: decimal<usd/mile> }

type RequestedPayment =
    { TransactionId: TransactionId 
      RequestedAt: DateTimeOffset 
      RequestedAmount: USD }

type SettledPayment =
    { TransactionId: TransactionId
      SettledAt: DateTimeOffset }

type ReturnedPayment =
    { TransactionId: TransactionId
      ReturnedAt: DateTimeOffset
      ReturnedReason: string }

type ReturnedVehicle =
    { VehicleId: VehicleId
      ReturnedAt: DateTimeOffset
      VehicleOdometer: Mile
      DamageCharge: USD }

[<RequireQualifiedAccess>]
type PaymentState =
    | Requested of
        {| PaymentAmount: USD
           RequestedAt: DateTimeOffset |}
    | Settled of
        {| PaymentAmount: USD
           RequestedAt: DateTimeOffset
           SettledAt: DateTimeOffset |}
    | Received of
        {| PaymentAmount: USD
           RequestedAt: DateTimeOffset
           SettledAt: DateTimeOffset
           ReceivedAt: DateTimeOffset |}
    | Returned of
        {| PaymentAmount: USD
           RequestedAt: DateTimeOffset
           SettledAt: DateTimeOffset
           ReceivedAt: DateTimeOffset option
           ReturnedAt: DateTimeOffset
           ReturnedReason: string |}

type LeaseCommand =
    | AcceptLease of AcceptedLease
    | RequestPayment of RequestedPayment
    | SettlePayment of SettledPayment
    | ReturnPayment of ReturnedPayment
    | ReturnVehicle of ReturnedVehicle

type StoredLeaseEvent =
    | LeaseAccepted of {| EventContext: EventContext; AcceptedLease: AcceptedLease |}
    | PaymentRequested of {| EventContext: EventContext; RequestedPayment: RequestedPayment |}
    | PaymentSettled of {| EventContext: EventContext; SettledPayment: SettledPayment |}
    | PaymentReturned of {| EventContext: EventContext; ReturnedPayment: ReturnedPayment |}
    | VehicleReturned of {| EventContext: EventContext; ReturnedVehicle: ReturnedVehicle |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseEvent =
    | DayStarted of EventContext
    | StoredLeaseEvent of StoredLeaseEvent
    | DayEnded of EventContext

type LeaseState =
    { Observation: Observation option
      AcceptedLease: AcceptedLease option
      ReturnedVehicle: ReturnedVehicle option
      Payments: Map<TransactionId,PaymentState>
      DaysPastDue: Day
      PaymentAmountScheduled: USD
      PaymentAmountScheduledHistory: (DateTimeOffset * USD) list
      PaymentAmountReceived: USD
      PaymentAmountChargedOff: USD
      PaymentAmountOutstanding: USD
      PaymentAmountCredit: USD
      PaymentAmountUnpaid: USD }

type StoredLeaseStream = StoredLeaseEvent list

type LeaseStream = LeaseEvent list
