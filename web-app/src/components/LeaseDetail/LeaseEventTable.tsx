import React, { useState } from 'react'
import gql from 'graphql-tag'
import { useQuery, useMutation } from '@apollo/react-hooks'
import MaterialTable, { Column } from 'material-table'
import { LinearProgress } from '@material-ui/core'
import { grey } from '@material-ui/core/colors'
import {
    Query,
    LeaseEvent,
    MutationDeleteLeaseEventArgs,
    ListLeaseEventsInput
} from '../../generated/graphql'

const LIST_LEASE_EVENTS = gql`
query ListLeaseEvents($input: ListLeaseEventsInput!) {
    listLeaseEvents(input: $input) {
        events {
            eventId
            eventCreatedTime
            eventEffectiveDate
            eventType
        }
        prevPageToken
        nextPageToken
        totalCount
    }
}`

const DELETE_LEASE_EVENT = gql`
mutation DeleteLeaseEvent($input: DeleteLeaseEventInput) {
    deleteLeaseEvent(input: $input)
}`

type LeaseEventTableProps = {
  leaseId: string,
  leaseEvents: LeaseEvent[]
}

export const LeaseEventTable: React.FC<LeaseEventTableProps> = ({ leaseId }) => {

    const [page, setPage] = useState(1)
    const [pageSize, setPageSize] = useState(5)
    const [pageToken, setPageToken] = useState("")
    const [prevPageToken, setPrevPageToken] = useState("")
    const [nextPageToken, setNextPageToken] = useState("")
    const { data, loading, error } = useQuery<Query,ListLeaseEventsInput>(
        LIST_LEASE_EVENTS,
        { variables: { leaseId, pageSize, pageToken } })
    const [deleteLeaseEvent] = useMutation<string,MutationDeleteLeaseEventArgs>(DELETE_LEASE_EVENT)

    const columns: Column<LeaseEvent>[] = [
        { title: "Event ID", field: "eventId"},
        { title: "Event Created Time", field: "eventCreatedTime"},
        { title: "Event Effective Date", field: "eventEffectiveDate"},
        { title: "Event Type", field: "eventType"}
    ]

    const handleChangePage = (newPage:number) => {
      if (newPage > page) {
        setPage(newPage)
        setPageToken(nextPageToken)
      }
      else {
        setPage(newPage)
        setPageToken(prevPageToken)
      }
    }

    const handleChangeRowsPerPage = (newPageSize:number) => {
      setPageSize(newPageSize)
    }


    if (loading) return <LinearProgress />
    if (error) return (<p>`Error!: ${error.message}`</p>)
    if (data) {
        setPrevPageToken(data.listLeases.prevPageToken)
        setNextPageToken(data.listLeases.nextPageToken)
    }
    return (
        <MaterialTable
            title="Lease Events"
            style={{
                backgroundColor: grey[600],
                padding: 10,
            }}
            columns={columns}
            onChangePage={handleChangePage}
            onChangeRowsPerPage={handleChangeRowsPerPage}
            data={data ? data.listLeaseEvents.events : []}
            options={{
                search: false,
                headerStyle: {
                    backgroundColor: grey[600],
                },
            }}
            editable={{
                onRowDelete: row =>
                    deleteLeaseEvent({
                        variables: {
                            input: {
                                leaseId,
                                eventId: row.eventId
                            }
                        }
                    }).then()
            }} />
    )
}
