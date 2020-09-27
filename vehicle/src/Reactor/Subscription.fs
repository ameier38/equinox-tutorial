namespace Reactor

open EventStore.ClientAPI
open FSharp.Control
open FSharp.UMX
open Serilog
open System
open System.Threading
open System.Threading.Tasks

type EventHandler = FsCodec.ITimelineEvent<byte[]> -> Async<unit>

[<RequireQualifiedAccess>]
type SubscriptionStatus =
    | Subscribed
    | Unsubscribed

type SubscriptionState =
    { Checkpoint: Checkpoint
      CancellationToken: CancellationToken
      SubscriptionStatus: SubscriptionStatus }

type SubscriptionMessage =
    | Subscribe
    | Subscribed of EventStoreStreamCatchUpSubscription
    | Dropped of SubscriptionDropReason * Exception
    | EventAppeared of Checkpoint
    | GetState of AsyncReplyChannel<SubscriptionState>

type SubscriptionMailbox = MailboxProcessor<SubscriptionMessage>

type Subscription(name:string, stream:Stream, eventHandler:EventHandler, eventstore:Store.EventStore) =
    let log = Log.ForContext<Subscription>()

    let settings =
        CatchUpSubscriptionSettings(
            maxLiveQueueSize = 100,
            readBatchSize = 10,
            verboseLogging = false,
            resolveLinkTos = true,
            subscriptionName = name)

    let subscribe (state:SubscriptionState) (mailbox:SubscriptionMailbox): Async<unit> =
        async {
            let eventAppeared =
                Func<EventStoreCatchUpSubscription,ResolvedEvent,Task>(fun _ resolvedEvent -> 
                    let work =
                        async {
                            let encodedEvent = UnionEncoderAdapters.encodedEventOfResolvedEvent resolvedEvent
                            let checkpoint = Checkpoint.StreamPosition encodedEvent.Index
                            do! eventHandler encodedEvent
                            mailbox.Post(EventAppeared checkpoint)
                        }
                    Async.StartAsTask(work, cancellationToken = state.CancellationToken) :> Task)
            let subscriptionDropped =
                Action<EventStoreCatchUpSubscription,SubscriptionDropReason,exn>(fun _ reason error ->
                    mailbox.Post(Dropped (reason, error)))
            log.Information("subscribing to {Stream} from checkpoint {Checkpoint}", stream, state.Checkpoint)
            // ref: https://eventstore.com/docs/projections/system-projections/index.html?tabs=tabid-5#by-category
            let subscription =
                let lastCheckpoint =
                    match state.Checkpoint with
                    | Checkpoint.StreamStart -> StreamCheckpoint.StreamStart
                    | Checkpoint.StreamPosition pos -> Nullable(pos)
                eventstore.Connection.SubscribeToStreamFrom(
                    stream = %stream,
                    lastCheckpoint = lastCheckpoint,
                    settings = settings,
                    eventAppeared = eventAppeared,
                    subscriptionDropped = subscriptionDropped,
                    userCredentials = eventstore.Credentials)
            mailbox.Post(Subscribed subscription)
        }

    let evolve 
        (mailbox:SubscriptionMailbox)
        : SubscriptionState -> SubscriptionMessage -> SubscriptionState =
        fun state msg ->
            match msg with
            | Subscribe ->
                match state.SubscriptionStatus with
                | SubscriptionStatus.Unsubscribed ->
                    Async.Start(subscribe state mailbox, state.CancellationToken)
                    state
                | _ -> state
            | Subscribed _ ->
                { state with SubscriptionStatus = SubscriptionStatus.Subscribed }
            | Dropped (reason, error) ->
                match reason with
                | SubscriptionDropReason.ServerError
                | SubscriptionDropReason.EventHandlerException
                | SubscriptionDropReason.ProcessingQueueOverflow
                | SubscriptionDropReason.CatchUpError
                | SubscriptionDropReason.ConnectionClosed ->
                    log.Information(error, "subscription dropped: {Reason}; reconnecting...", reason)
                    mailbox.Post(Subscribe)
                | _ ->
                    log.Error(error, "subscription dropped {Reason}", reason)
                    raise error
                { state with SubscriptionStatus = SubscriptionStatus.Unsubscribed }
            | EventAppeared checkpoint ->
                { state with Checkpoint = checkpoint }
            | GetState channel ->
                channel.Reply(state)
                state

    let start (initialState:SubscriptionState) =
        let mailbox = SubscriptionMailbox.Start(fun inbox ->
            AsyncSeq.initInfiniteAsync(fun _ -> inbox.Receive())
            |> AsyncSeq.fold (evolve inbox) initialState
            |> Async.Ignore)
        mailbox.Post(Subscribe)
        mailbox

    let rec loop (mailbox:SubscriptionMailbox) =
        async {
            let! state = mailbox.PostAndAsyncReply(GetState)
            log.Debug("stream {Stream} is at checkpoint {Checkpoint}", stream, state.Checkpoint)
            do! Async.Sleep 5000
            return! loop mailbox
        }

    member _.SubscribeAsync(checkpoint:Checkpoint, token:CancellationToken) =
        async {
            let initialState =
                { Checkpoint = checkpoint
                  CancellationToken = token
                  SubscriptionStatus = SubscriptionStatus.Unsubscribed }
            let mailbox = start initialState
            do! loop mailbox
        }
        