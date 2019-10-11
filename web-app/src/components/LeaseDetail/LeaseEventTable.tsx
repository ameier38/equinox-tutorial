import React, { useState } from 'react'
import { useMutation, UseQueryResult, UseClientRequestResult } from 'graphql-hooks'
import { 
    IconButton,
    LinearProgress,
    Paper,
    Table,
    TableHead,
    TableBody,
    TableRow,
    TableCell,
    TableFooter,
    TablePagination
} from '@material-ui/core'
import { Delete } from '@material-ui/icons'
import { makeStyles, createStyles } from '@material-ui/core/styles'
import {
    Query,
    LeaseEvent,
    MutationDeleteLeaseEventArgs,
} from '../../generated/graphql'
import { Event } from './types'

const useStyles = makeStyles(() =>
  createStyles({
    tableRoot: {
      margin: 10,
    },
  })
)

const DELETE_LEASE_EVENT = `
mutation DeleteLeaseEvent($input: DeleteLeaseEventInput) {
    deleteLeaseEvent(input: $input)
}`

type Column = {
    title: string,
    field: keyof LeaseEvent,
}

type LeaseEventTableProps = {
    leaseId: string,
    pageSize: number,
    listLeaseEventsResult: UseClientRequestResult<Query>,
    dispatch: React.Dispatch<Event> 
}

export const LeaseEventTable: React.FC<LeaseEventTableProps> = ({ 
    leaseId,
    pageSize,
    listLeaseEventsResult,
    dispatch
}) => {
    const classes = useStyles()
    const [page, setPage] = useState(0)
    const [deleteLeaseEvent] = useMutation<string,MutationDeleteLeaseEventArgs>(DELETE_LEASE_EVENT)
    let { data, loading, error } = listLeaseEventsResult

    const columns: Column[] = [
        { title: "Event ID", field: "eventId"},
        { title: "Event Created Time", field: "eventCreatedTime"},
        { title: "Event Effective Date", field: "eventEffectiveDate"},
        { title: "Event Type", field: "eventType"},
    ]

    const handleChangePage = (prevToken:string, nextToken:string) => (evt:unknown, newPage:number) => {
      if (newPage > page) {
        setPage(newPage)
        dispatch({type: 'LEASE_EVENTS_PAGE_TOKEN_UPDATED', pageToken: nextToken})
      }
      else {
        setPage(newPage)
        dispatch({type: 'LEASE_EVENTS_PAGE_TOKEN_UPDATED', pageToken: prevToken})
      }
    }

    const handleChangeRowsPerPage = (evt:React.ChangeEvent<HTMLInputElement>) => {
        dispatch({type: 'LEASE_EVENTS_PAGE_SIZE_UPDATED', pageSize: +evt.target.value})
    }

    const handleDeleteLeaseEvent = (eventId:number) => {
        return deleteLeaseEvent({
            variables: {
                input: {
                    leaseId,
                    eventId
                }
            }
        }).then(() => {
            return dispatch({type: 'RESET_TOGGLED', reset: true})
        })
    }

    if (loading) return <LinearProgress />
    if (error) return (<p>`Error!: ${error}`</p>)
    return (
        <Paper className={classes.tableRoot}>
            { !loading && data && 
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>Delete</TableCell>
                        {columns.map((col, idx) => (
                            <TableCell key={idx} align='center'>{col.title}</TableCell>
                        ))}
                    </TableRow>
                </TableHead>
                <TableBody>
                    {data.listLeaseEvents.events.map((event, rIdx) => (
                        <TableRow key={rIdx} >
                            <TableCell>
                                <IconButton onClick={() => handleDeleteLeaseEvent(event.eventId)}>
                                    <Delete />
                                </IconButton>
                            </TableCell>
                            {columns.map((col, cIdx) => (
                                <TableCell key={cIdx} align='center'>{event[col.field]}</TableCell>
                            ))}
                        </TableRow>
                    ))}
                </TableBody>
                <TableFooter>
                    <TableRow>
                        <TablePagination
                            rowsPerPageOptions={[5, 10, 20]}
                            count={data.listLeaseEvents.totalCount}
                            page={page}
                            rowsPerPage={pageSize}
                            onChangePage={handleChangePage(data.listLeaseEvents.prevPageToken, data.listLeaseEvents.nextPageToken)}
                            onChangeRowsPerPage={handleChangeRowsPerPage} />
                    </TableRow>
                </TableFooter>
            </Table>
            }
        </Paper>
    )
}
