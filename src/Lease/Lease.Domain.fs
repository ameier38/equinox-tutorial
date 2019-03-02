namespace Lease

open System
open FSharp.UMX

type Context =
    { EventId: EventId
      CreatedDate: CreatedDate
      EffectiveDate: EffectiveDate }
module Context =
    let create eventId effectiveDate =
        { EventId = eventId
          CreatedDate = %DateTime.UtcNow
          EffectiveDate = effectiveDate }

type Lease =
    { LeaseId: LeaseId
      StartDate: DateTime
      MaturityDate: DateTime
      MonthlyPaymentAmount: decimal }

type Payment =
    { PaymentDate: DateTime
      PaymentAmount: decimal }

type LeaseCommand =
    | Undo of EventId
    | Create of Lease
    | Modify of Lease * EffectiveDate
    | SchedulePayment of Payment
    | ReceivePayment of Payment
    | Terminate of EffectiveDate

type LeaseInfo =
    { Lease: Lease
      Context: Context }

type PaymentInfo =
    { Payment: Payment
      Context: Context }

type LeaseEvent =
    | Undid of EventId
    | Compacted of LeaseEvent[]
    | Created of LeaseInfo
    | Modified of LeaseInfo
    | PaymentScheduled of PaymentInfo
    | PaymentReceived of PaymentInfo
    | Terminated of Context
    interface TypeShape.UnionContract.IUnionContract
module LeaseEvent =
    let (|Order|) { CreatedDate = createdDate; EffectiveDate = effDate } = (effDate, createdDate)
    let getContext = function
        | Undid _ -> None
        | Compacted _ -> None
        | Created { Context = ctx } -> ctx |> Some
        | Modified { Context = ctx } -> ctx |> Some
        | PaymentScheduled { Context = ctx } -> ctx |> Some
        | PaymentReceived { Context = ctx } -> ctx |> Some
        | LeaseEvent.Terminated ctx -> ctx |> Some
    let getOrder = getContext >> Option.map (fun (Order order) -> order)
    let getEventId = getContext >> Option.map (fun { EventId = eventId } -> eventId)

type LeaseStateData =
    { Lease: Lease
      TotalScheduled: decimal
      TotalPaid: decimal
      AmountDue: decimal
      CreatedDate: DateTime
      ModifiedDate: DateTime }

type LeaseState =
    | NonExistent
    | Corrupt of string
    | Outstanding of LeaseStateData
    | Terminated of LeaseStateData
