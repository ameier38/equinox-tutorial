namespace Reactor

open EventStore.ClientAPI
open FSharp.UMX
open System

type [<Measure>] vehicleId
type VehicleId = string<vehicleId>

type [<Measure>] stream
type Stream = string<stream>

[<RequireQualifiedAccess>]
type Checkpoint =
    | StreamStart
    | StreamPosition of int64

module UnionEncoderAdapters =
    let encodedEventOfResolvedEvent (resolvedEvent:ResolvedEvent): FsCodec.ITimelineEvent<byte[]> =
        let event = resolvedEvent.Event
        let ts = DateTimeOffset.FromUnixTimeMilliseconds(event.CreatedEpoch)
        FsCodec.Core.TimelineEvent.Create(
            resolvedEvent.OriginalEventNumber,
            event.EventType,
            event.Data,
            event.Metadata,
            event.EventId,
            timestamp = ts)
