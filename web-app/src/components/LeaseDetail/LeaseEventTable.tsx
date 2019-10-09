import React, { useState } from 'react'
import { useHistory } from 'react-router'
import { useMutation, UseQueryResult } from 'graphql-hooks'
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
import { grey } from '@material-ui/core/colors'
import {
    Query,
    LeaseEvent,
    MutationDeleteLeaseEventArgs,
    QueryListLeaseEventsArgs
} from '../../generated/graphql'

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
    setPageToken: (pageToken:string) => void,
    setPageSize: (pageSize:number) => void,
    pageSize: number,
    listLeaseEventsResult: UseQueryResult<Query,QueryListLeaseEventsArgs>
}

export const LeaseEventTable: React.FC<LeaseEventTableProps> = ({ 
    leaseId,
    setPageSize,
    setPageToken,
    pageSize,
    listLeaseEventsResult
}) => {
    const classes = useStyles()
    const [page, setPage] = useState(0)
    const [deleteLeaseEvent] = useMutation<string,MutationDeleteLeaseEventArgs>(DELETE_LEASE_EVENT)
    let { data, loading, error, refetch } = listLeaseEventsResult

    const columns: Column[] = [
        { title: "Event ID", field: "eventId"},
        { title: "Event Created Time", field: "eventCreatedTime"},
        { title: "Event Effective Date", field: "eventEffectiveDate"},
        { title: "Event Type", field: "eventType"},
    ]

    const handleChangePage = (prevToken:string, nextToken:string) => (evt:unknown, newPage:number) => {
      if (newPage > page) {
        setPage(newPage)
        setPageToken(nextToken)
      }
      else {
        setPage(newPage)
        setPageToken(prevToken)
      }
    }

    const handleChangeRowsPerPage = (evt:React.ChangeEvent<HTMLInputElement>) => {
      setPageSize(+evt.target.value)
    }

    const handleDeleteLeaseEvent = (eventId:number) => {
        deleteLeaseEvent({
            variables: {
                input: {
                    leaseId,
                    eventId
                }
            }
        }).then(() => {
            return refetch()
        })
    }

    if (loading) return <LinearProgress />
    if (error) return (<p>`Error!: ${error}`</p>)
    return (
        <Paper className={classes.tableRoot}>
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
                            <TableCell 
                                onClick={() => deleteLeaseEvent({ 
                                    variables: { input: { leaseId, eventId: event.eventId } } })}>
                                <IconButton>
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
        </Paper>
    )
}
