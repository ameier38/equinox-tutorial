import React, { useState } from 'react';
import { useHistory } from 'react-router-dom'
import { makeStyles, createStyles } from '@material-ui/core/styles'
import {
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
import { UseQueryResult } from 'graphql-hooks';
import { Query, QueryListLeasesArgs, Lease } from '../../generated/graphql';

const useStyles = makeStyles(() =>
  createStyles({
    container: {
      paddingTop: 20,
    },
    tableRoot: {
      margin: 10,
    },
    row: {
        cursor: 'pointer'
    }
  })
)

type LeaseTableProps = {
    setPageToken: (pageToken:string) => void,
    setPageSize: (pageSize:number) => void,
    pageSize: number,
    listLeasesResult: UseQueryResult<Query,QueryListLeasesArgs>
}

type Column = {
    title: string,
    field: keyof Lease,
}

export const LeaseTable: React.FC<LeaseTableProps> = ({
    setPageToken,
    setPageSize,
    pageSize,
    listLeasesResult
}) => {
    const classes = useStyles();
    const history = useHistory()
    const [page, setPage] = useState(0)
    const { data, loading, error} = listLeasesResult

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

    const columns: Column[] = [
        { title: 'Lease ID', field: 'leaseId' },
        { title: 'Start Date', field: 'startDate' },
        { title: 'Maturity Date', field: 'maturityDate' },
        { title: 'Monthly Payment Amount', field: 'monthlyPaymentAmount' },
    ]

    if (loading) return <LinearProgress />
    if (error) return (<p>{`Error!: ${error}`}</p>)
    return (
        <Paper className={classes.tableRoot}>
            <Table>
                <TableHead>
                    <TableRow>
                        {columns.map((col, idx) => (
                            <TableCell key={idx} align='center'>{col.title}</TableCell>
                        ))}
                    </TableRow>
                </TableHead>
                <TableBody>
                    {data.listLeases.leases.map((lease, rIdx) => (
                        <TableRow className={classes.row} key={rIdx} hover onClick={() => history.push(`/${lease.leaseId}`)}>
                            {columns.map((col, cIdx) => (
                                <TableCell key={cIdx} align='center'>{lease[col.field]}</TableCell>
                            ))}
                        </TableRow>
                    ))}
                </TableBody>
                <TableFooter>
                    <TableRow>
                        <TablePagination
                            rowsPerPageOptions={[5, 10, 20]}
                            count={data.listLeases.totalCount}
                            page={page}
                            rowsPerPage={pageSize}
                            onChangePage={handleChangePage(data.listLeases.prevPageToken, data.listLeases.nextPageToken)}
                            onChangeRowsPerPage={handleChangeRowsPerPage} />
                    </TableRow>
                </TableFooter>
            </Table>
        </Paper>
    )
}
