namespace Lease

open System

type AsOn =
    { AsAt: CreatedDate 
      AsOf: EffectiveDate }

type EventContext =
    { CreatedAt: CreatedDate
      EffectiveAt: EffectiveDate }

type AcceptedLease =
    { UserId: UserId
      VehicleId: VehicleId
      AcceptedAt: DateTimeOffset
      StartDate: DateTimeOffset 
      MaturityDate: DateTimeOffset
      InterestRate: decimal<1/year>
      SalesTax: decimal
      VehicleValue: USD
      ResidualPercentage: decimal
      DownPayment: USD
      TradeInValue: USD
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
      RejectedAmount: USD }

type SettledPayment =
    { TransactionId: TransactionId
      SettledAt: DateTimeOffset
      SettledAmount: USD }

type ReturnedPayment =
    { TransactionId: TransactionId
      ReturnedAt: DateTimeOffset
      ReturnedAmount: USD }

type ReturnedVehicle =
    { VehicleId: VehicleId
      VehicleOdometer: Mile
      DamageCharge: USD }

type LeaseCommand =
    | AcceptLease of AcceptedLease
    | RequestPayment of RequestedPayment
    | RejectPayment of RejectedPayment
    | SettlePayment of SettledPayment
    | ReturnPayment of ReturnedPayment
    | ReturnVehicle of ReturnedVehicle

type LeaseEvent =
    | LeaseAccepted of {| EventContext: EventContext; AcceptedLease: AcceptedLease |}
    | PaymentRequested of {| EventContext: EventContext; RequestedPayment: RequestedPayment |}
    | PaymentRejected of {| EventContext: EventContext; RejectedPayment: RejectedPayment |}
    | PaymentSettled of {| EventContext: EventContext; SettledPayment: SettledPayment |}
    | PaymentReturned of {| EventContext: EventContext; ReturnedPayment: ReturnedPayment |}
    | VehicleReturned of {| EventContext: EventContext; ReturnedVehicle: ReturnedVehicle |}
    interface TypeShape.UnionContract.IUnionContract

type LeaseStatus =
    | Requested
    | Accepted

type LeaseObservation =
    { LeaseId: LeaseId
      ObservedAt: EffectiveDate
      EventOrder: EventOrder
      EventType: EventType
      AcceptedLease: AcceptedLease
      ReturnedVehicle: ReturnedVehicle option
      PendingPayments: SettledPayment list
      CumPaymentAmountScheduled: USD
      CumPaymentAmountRequested: USD
      CumPaymentAmountRejected: USD
      CumPaymentAmountSettled: USD
      CumPaymentAmountReturned: USD
      CumPaymentAmountChargedOff: USD
      DaysPastDue: USD
      OutstandingPaymentAmount: USD
      UnpaidPaymentAmount: USD }

type LeaseState = LeaseObservation option

type LeaseStream = LeaseEvent list
