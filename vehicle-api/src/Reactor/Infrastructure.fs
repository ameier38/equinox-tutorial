namespace Reactor

open EventStore.ClientAPI
open FSharp.UMX
open FsCodec
open Serilog
open System

type [<Measure>] vehicleId
type VehicleId = string<vehicleId>

type StreamEvent = { StreamName: StreamName; Event: ITimelineEvent<byte[]> }

module UnionEncoderAdapters =
    let encodedEventOfResolvedEvent (resolvedEvent:ResolvedEvent): StreamEvent =
        let event = resolvedEvent.Event
        Log.Information("event number {EventNumber}", resolvedEvent.OriginalEventNumber)
        let streamName = StreamName.parse event.EventStreamId
        let ts = DateTimeOffset.FromUnixTimeMilliseconds(event.CreatedEpoch)
        let timelineEvent =
            Core.TimelineEvent.Create(
                resolvedEvent.OriginalEventNumber,
                event.EventType,
                event.Data,
                event.Metadata,
                event.EventId,
                timestamp = ts)
        { StreamName = streamName; Event = timelineEvent }
