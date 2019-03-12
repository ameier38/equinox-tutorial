namespace Lease

open Equinox.EventStore
open Serilog

type Service =
    { execute: LeaseId -> LeaseCommand -> AsyncResult<unit,string>
      query: LeaseId -> ObservationDate -> Async<LeaseState> }
module Service =
    let init 
        (aggregate:Aggregate) 
        (resolver:GesResolver<LeaseEvent,LeaseEvents>) =
        let log = LoggerConfiguration().WriteTo.Console().CreateLogger()
        let (|AggregateId|) (leaseId: LeaseId) = Equinox.AggregateId(aggregate.entity, LeaseId.toStringN leaseId)
        let (|Stream|) (AggregateId leaseId) = Equinox.Stream(log, resolver.Resolve leaseId, 3)
        let execute (Stream stream) command = stream.Transact(aggregate.interpret command)
        let query (Stream stream) (obsDate:ObservationDate) =
            stream.Query(fun leaseEvents -> 
                leaseEvents 
                |> aggregate.reconstitute obsDate)
        { execute = execute
          query = query }
