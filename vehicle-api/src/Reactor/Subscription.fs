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

type Subscription(name:string, stream:Stream, eventHandler:EventHandler, eventstoreConfig:EventStoreConfig) =
    let log = Log.ForContext("SubscriptionId", name)
    let eventstore = EventStoreConnection.Create(Uri(eventstoreConfig.Url))
    do eventstore.ConnectAsync().Wait()
    do log.Information("🐲 Connected to EventStore at {Url}", eventstoreConfig.Url)

    let settings =
        CatchUpSubscriptionSettings(
            maxLiveQueueSize = 10,
            readBatchSize = 10,
            verboseLogging = false,
            resolveLinkTos = true,
            subscriptionName = "vehicles")
    let credentials =
        SystemData.UserCredentials(
            username = eventstoreConfig.User,
            password = eventstoreConfig.Password)

    let subscribe (state:SubscriptionState) (mailbox:SubscriptionMailbox): Async<unit> =
        async {
            let eventAppeared =
                Func<EventStoreCatchUpSubscription,ResolvedEvent,Task>(fun _ resolvedEvent -> 
                    let work =
                        async {
                            let encodedEvent = UnionEncoderAdapters.encodedEventOfResolvedEvent resolvedEvent
                            let checkpoint = UMX.tag<checkpoint> encodedEvent.Index
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
                eventstore.SubscribeToStreamFrom(
                    stream = %stream,
                    lastCheckpoint = new Nullable<int64>(%state.Checkpoint),
                    settings = settings,
                    eventAppeared = eventAppeared,
                    subscriptionDropped = subscriptionDropped,
                    userCredentials = credentials)
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
        