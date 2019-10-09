import React, { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { useManualQuery, useQuery } from 'graphql-hooks'
import Plot from 'react-plotly.js'
import { LinearProgress, CircularProgress } from '@material-ui/core'
import { AsOfDateSlider } from './AsOfDateSlider'
import { CommandPanel } from './CommandPanel'
import { LeaseEventTable } from './LeaseEventTable'
import { ReceivePaymentDialog } from './ReceivePaymentDialog'
import { SchedulePaymentDialog } from './SchedulePaymentDialog'
import { 
    Query, 
    QueryGetLeaseArgs, 
    AsOfDate,
    QueryListLeaseEventsArgs
} from '../../generated/graphql'

const GET_LEASE = `
query GetLease($input: GetLeaseInput!) {
    getLease(input: $input) {
        lease {
            startDate
            maturityDate
        }
        createdTime
        updatedTime
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
    const [fetchLease, { data, loading, error }] = useManualQuery<Query,QueryGetLeaseArgs>(
        GET_LEASE,
        { variables: { input: { leaseId }}})
    const listLeaseEventsResult = useQuery<Query,QueryListLeaseEventsArgs>(
        LIST_LEASE_EVENTS,
        { variables: { input: { leaseId, pageSize, pageToken } } })

    const xData = data ? [
        data.getLease.totalScheduled,
        data.getLease.totalPaid,
        data.getLease.amountDue
    ] : []
    const yData = data ? [
        'Total Scheduled',
        'Total Paid',
        'Amount Due'
    ] : []

    useEffect(() => {
        fetchLease()
    }, [asAt, asOn])

    if (error) return <p>{JSON.stringify(error)}</p>

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
            {loading || !data ? <LinearProgress /> : 
                <AsOfDateSlider 
                    startDate={new Date(data.getLease.lease.startDate)}
                    updatedTime={new Date(data.getLease.updatedTime)}
                    asAt={asAt}
                    setAsAt={setAsAt}
                    asOn={asOn}
                    setAsOn={setAsOn} /> 
            }
            <CommandPanel
                setReceivePaymentDialogOpen={setReceivePaymentDialogOpen}
                setSchedulePaymentDialogOpen={setSchedulePaymentDialogOpen} />
            <LeaseEventTable 
                leaseId={leaseId}
                setPageSize={setPageSize}
                setPageToken={setPageToken}
                pageSize={pageSize}
                listLeaseEventsResult={listLeaseEventsResult} />
            <ReceivePaymentDialog 
                leaseId={leaseId}
                open={receivePaymentDialogOpen}
                setOpen={setReceivePaymentDialogOpen} />
            <SchedulePaymentDialog 
                leaseId={leaseId}
                open={schedulePaymentDialogOpen}
                setOpen={setSchedulePaymentDialogOpen} />
        </React.Fragment>
    )
}
