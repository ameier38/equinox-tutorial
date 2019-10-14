import React, { useReducer, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { useManualQuery } from 'graphql-hooks'
import { AsOfSlider } from './AsOfSlider'
import { CommandPanel } from './CommandPanel'
import { BalancePlot } from './BalancePlot'
import { LeaseEventTable } from './LeaseEventTable'
import { ReceivePaymentDialog } from './ReceivePaymentDialog'
import { SchedulePaymentDialog } from './SchedulePaymentDialog'
import { 
    Query, 
    QueryGetLeaseArgs, 
    QueryListLeaseEventsArgs,
} from '../../generated/graphql'
import { State, Event } from './types'
import moment from 'moment'

const GET_LEASE = `
query GetLease($input: GetLeaseInput!) {
    getLease(input: $input) {
        createdAtTime
        updatedAtTime
        updatedOnDate
        leaseId
        userId
        totalScheduled
        totalPaid
        amountDue
        leaseStatus
    }
}`

const LIST_LEASE_EVENTS = `
query ListLeaseEvents($input: ListLeaseEventsInput!) {
    listLeaseEvents(input: $input) {
        events {
            eventId
            eventCreatedTime
            eventEffectiveDate
            eventType
            eventPayload
        }
        prevPageToken
        nextPageToken
        totalCount
    }
}`

type RouteParams = { leaseId: string }

const initialState : State = {
    shouldReset: true,
    asOf: { 
        asAt: moment.utc().toDate(),
        asOn: moment.utc().toDate()
    },
    leaseEvents: [],
    leaseEventsPageSize: 10,
    leaseEventsPageToken: "",
    schedulePaymentDialogOpen: false,
    receivePaymentDialogOpen: false
}

const reducer = (state:State, event:Event) : State => {
    switch(event.type) {
        case 'RESET_TOGGLED':
            console.log(event, state)
            const newState = {...state, shouldReset: event.reset}
            console.log(newState)
            return newState
        case 'LEASE_EVENTS_RESET':
            return {...state, leaseEvents: event.leaseEvents}
        case 'AS_OF_UPDATED':
            const asAt = event.asOf.asAt ? event.asOf.asAt : state.asOf.asAt
            const asOn = event.asOf.asOn ? event.asOf.asOn : state.asOf.asOn
            return {...state, asOf: { asAt, asOn }}
        case 'LEASE_EVENTS_PAGE_SIZE_UPDATED':
            return {...state, leaseEventsPageSize: event.pageSize}
        case 'LEASE_EVENTS_PAGE_TOKEN_UPDATED':
            return {...state, leaseEventsPageToken: event.pageToken}
        case 'SCHEDULE_PAYMENT_DIALOG_TOGGLED':
            return {...state, schedulePaymentDialogOpen: event.open}
        case 'RECEIVE_PAYMENT_DIALOG_TOGGLED':
            return {...state, receivePaymentDialogOpen: event.open}
        default:
            throw new Error()
    }
}

export const LeaseDetail = () => {
    const { leaseId } = useParams<RouteParams>()
    const [state, dispatch] = useReducer(reducer, initialState)

    const asOf = {
        asAt: state.asOf.asAt.toISOString(),
        asOn: state.asOf.asOn.toISOString()
    }

    const getLeaseInput = {
        leaseId,
        asOf
    }

    const listLeaseEventsInput = {
        leaseId,
        pageSize: state.leaseEventsPageSize, 
        pageToken: state.leaseEventsPageToken,
        asOf
    }

    const [getLease, getLeaseResult] = useManualQuery<Query,QueryGetLeaseArgs>(
        GET_LEASE,
        { variables: { input: getLeaseInput } })

    const [listLeaseEvents, listLeaseEventsResult] = useManualQuery<Query,QueryListLeaseEventsArgs>(
        LIST_LEASE_EVENTS,
        { variables: { input: listLeaseEventsInput } })

    useEffect(() => {
        if (state.shouldReset) {
            getLease()
            listLeaseEvents().then(({data}) => {
                dispatch({type: 'LEASE_EVENTS_RESET', leaseEvents: data.listLeaseEvents.events})
                dispatch({type: 'RESET_TOGGLED', reset: false})
            })
        } else {
            getLease()
            listLeaseEvents()
        }
    }, [state.asOf])

    useEffect(() => {
        listLeaseEvents()
    }, [state.leaseEventsPageSize, state.leaseEventsPageToken])

    return (
        <React.Fragment>
            <AsOfSlider 
                leaseEvents={state.leaseEvents}
                dispatch={dispatch} /> 
            <BalancePlot
                leaseId={leaseId}
                getLeaseResult={getLeaseResult} />
            <CommandPanel
                dispatch={dispatch} />
            <LeaseEventTable 
                leaseId={leaseId}
                pageSize={state.leaseEventsPageSize}
                listLeaseEventsResult={listLeaseEventsResult}
                dispatch={dispatch} />
            <ReceivePaymentDialog 
                leaseId={leaseId}
                open={state.receivePaymentDialogOpen}
                dispatch={dispatch} />
            <SchedulePaymentDialog 
                leaseId={leaseId}
                open={state.schedulePaymentDialogOpen}
                dispatch={dispatch} />
        </React.Fragment>
    )
}
