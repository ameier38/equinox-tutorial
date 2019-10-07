import React, { useState } from 'react';
import { useQuery } from '@apollo/react-hooks'
import gql from 'graphql-tag'
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import LinearProgress from '@material-ui/core/LinearProgress'
import MaterialTable, { Column } from 'material-table'
import { 
    Query,
    Lease,
    ListLeasesInput, 
} from '../generated/graphql'

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    container: {
      paddingTop: 20,
    },
    tableRoot: {
      margin: 10,
    },
    textField: {
      marginLeft: theme.spacing(1),
      marginRight: theme.spacing(1),
    },
  })
)

const LIST_LEASES = gql`
query ListLeases($input: ListLeasesInput!) {
    listLeases(input: $input) {
        leases {
            leaseId
            startDate
            maturityDate
            monthlyPaymentAmount
        }
        prevPageToken
        nextPageToken
        totalCount
    }
}
`

export const LeaseTable: React.FC = () => {
    const classes = useStyles();

    const [page, setPage] = useState(1)
    const [pageSize, setPageSize] = useState(10)
    const [pageToken, setPageToken] = useState("")
    const [prevPageToken, setPrevPageToken] = useState("")
    const [nextPageToken, setNextPageToken] = useState("")
    const { data, loading, error } = useQuery<Query,ListLeasesInput>(
        LIST_LEASES,
        { variables: {pageSize, pageToken}})

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

    const columns: Column<Lease>[] = [
      { title: "Lease ID", field: "leaseId" },
      { title: "Start Date", field: "startDate" },
      { title: "Maturity Date", field: "maturityDate" },
      { title: "Monthly Payment Amount", field: "monthlyPaymentAmount" },
    ]

    if (loading) return <LinearProgress />
    if (error) return (<p>`Error!: ${error.message}`</p>)
    if (data) {
        setPrevPageToken(data.listLeases.prevPageToken)
        setNextPageToken(data.listLeases.nextPageToken)
        return (
            <div className={classes.tableRoot}>
                <MaterialTable
                    options={{
                        search: false
                    }}
                    style={{
                        padding: 10,
                    }}
                    columns={columns}
                    data={data.listLeases.leases}
                    onChangePage={handleChangePage}
                    onChangeRowsPerPage={handleChangeRowsPerPage}
                    title="Leases" />
            </div>
        )
    }
    return <LinearProgress />
  }
