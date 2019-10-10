import React, { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { useManualQuery, useQuery } from 'graphql-hooks'
import Plot from 'react-plotly.js'
import { LinearProgress, CircularProgress } from '@material-ui/core'
import { AsOfSlider } from './AsOfSlider'
import { CommandPanel } from './CommandPanel'
import { LeaseEventTable } from './LeaseEventTable'
import { ReceivePaymentDialog } from './ReceivePaymentDialog'
import { SchedulePaymentDialog } from './SchedulePaymentDialog'
import { 
    Query, 
    QueryGetLeaseArgs, 
    AsOf,
    QueryListLeaseEventsArgs,
    LeaseEvent
} from '../../generated/graphql'

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

export const LeaseDetail = () => {
    const { leaseId } = useParams<RouteParams>()
    const [asAt, setAsAt] = useState(new Date())
    const [asOn, setAsOn] = useState(new Date())
    const [pageSize, setPageSize] = useState(5)
    const [pageToken, setPageToken] = useState("")
    const [schedulePaymentDialogOpen, setSchedulePaymentDialogOpen] = useState(false)
    const [receivePaymentDialogOpen, setReceivePaymentDialogOpen] = useState(false)
    const [currentLeaseEvents, setCurrentLeaseEvents] = useState<LeaseEvent[]>([])

    const getLeaseResult = useQuery<Query,QueryGetLeaseArgs>(
        GET_LEASE,
        { 
            variables: { 
                input: { 
                    leaseId,
                    asOf: {
                        asAt: asAt.toISOString(),
                        asOn: asOn.toISOString()
                    }
                } 
            } 
        })

    const [listLeaseEvents, listLeaseEventsResult] = useManualQuery<Query,QueryListLeaseEventsArgs>(
        LIST_LEASE_EVENTS,
        { 
            variables: { 
                input: { 
                    leaseId,
                    pageSize, 
                    pageToken,
                } 
            } 
        })

    const resetCurrentLeaseEvents = () => {
        listLeaseEvents().then(({data}) => {
            setCurrentLeaseEvents(data.listLeaseEvents.events)
        }) 
    }

    useEffect(() => {
        resetCurrentLeaseEvents()
    }, [])

    useEffect(() => {
        listLeaseEvents({
            variables: {
                input: { 
                    leaseId,
                    pageSize, 
                    pageToken,
                    asOf: {
                        asAt: asAt.toISOString(),
                        asOn: asOn.toISOString()
                    }
                } 
            }
        })
    }, [asAt, asOn])

    const refetch = () => {
        return Promise.all([getLeaseResult.refetch(), resetCurrentLeaseEvents()])
    }

    // const xData = data ? [
    //     data.getLease.totalScheduled,
    //     data.getLease.totalPaid,
    //     data.getLease.amountDue
    // ] : []
    // const yData = data ? [
    //     'Total Scheduled',
    //     'Total Paid',
    //     'Amount Due'
    // ] : []

    return (
        <React.Fragment>
            {/* {loading || (!leaseData) ? 
                <React.Fragment>
                    <Plot
                        data={[
                            { type: 'bar', x: xData, y: yData }
                        ]}
                        layout={{ width: 300, height: 300, title: leaseId }} />
                </React.Fragment>
                : <CircularProgress />} */}
            <AsOfSlider 
                leaseEvents={currentLeaseEvents}
                setAsAt={setAsAt}
                setAsOn={setAsOn} /> 
            <CommandPanel
                setReceivePaymentDialogOpen={setReceivePaymentDialogOpen}
                setSchedulePaymentDialogOpen={setSchedulePaymentDialogOpen} />
            <LeaseEventTable 
                leaseId={leaseId}
                setPageSize={setPageSize}
                setPageToken={setPageToken}
                pageSize={pageSize}
                listLeaseEventsResult={listLeaseEventsResult}
                refetch={refetch} />
            <ReceivePaymentDialog 
                leaseId={leaseId}
                open={receivePaymentDialogOpen}
                setOpen={setReceivePaymentDialogOpen}
                refetch={refetch} />
            <SchedulePaymentDialog 
                leaseId={leaseId}
                open={schedulePaymentDialogOpen}
                setOpen={setSchedulePaymentDialogOpen}
                refetch={refetch} />
        </React.Fragment>
    )
}
