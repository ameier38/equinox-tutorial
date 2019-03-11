namespace Lease

open Equinox.EventStore
open Serilog

type Service =
    { execute: LeaseId -> LeaseCommand -> AsyncResult<unit,string>
      query: LeaseId -> ObservationDate -> AsyncResult<LeaseState * EffectiveLeaseEvents,string> }
module Service =
    let init 
        (aggregate:Aggregate) 
        (resolver:GesResolver<LeaseEvent,EffectiveLeaseEvents>) =
        let log = LoggerConfiguration().WriteTo.Console().CreateLogger()
        let (|AggregateId|) (leaseId: LeaseId) = Equinox.AggregateId(aggregate.entity, LeaseId.toStringN leaseId)
        let (|Stream|) (AggregateId leaseId) = Equinox.Stream(log, resolver.Resolve leaseId, 3)
        let execute (Stream stream) command = stream.Transact(aggregate.interpret command)
        let query (Stream stream) (obsDate:ObservationDate) =
            stream.Query(fun effEvents ->
                let filteredEvents = effEvents |> aggregate.filterAtOrBefore obsDate
                let leaseState = filteredEvents |> aggregate.reconstitute
                (leaseState, filteredEvents))
            |> AsyncResult.ofAsync
        { execute = execute
          query = query }
