import React, { useState } from 'react'
import { useQuery } from 'graphql-hooks'
import { Fab } from '@material-ui/core'
import AddIcon from '@material-ui/icons/Add'
import { makeStyles, createStyles } from '@material-ui/styles'
import { LeaseTable } from './LeaseTable'
import { CreateLeaseDialog } from './CreateLeaseDialog'
import { 
    Query,
    QueryListLeasesArgs, 
} from '../../generated/graphql'

const useStyles = makeStyles(() =>
  createStyles({
    fab: {
      position: 'fixed',
      right: 10,
      bottom: 10,
      zIndex: 2000,
    }
  })
)

const LIST_LEASES = `
query ListLeases($input: ListLeasesInput!) {
    listLeases(input: $input) {
        leases {
            leaseId
            commencementDate
            expirationDate
            monthlyPaymentAmount
        }
        prevPageToken
        nextPageToken
        totalCount
    }
}`

export const LeaseSummary: React.FC = () => {
    const classes = useStyles()
    const [pageSize, setPageSize] = useState(10)
    const [pageToken, setPageToken] = useState("")
    const listLeasesResult = useQuery<Query,QueryListLeasesArgs>(
        LIST_LEASES,
        { variables: { input: { pageSize, pageToken } } })
    const [createLeaseDialogOpen, setCreateLeaseDialogOpen] = useState(false)
    return (
        <React.Fragment>
            <LeaseTable
                setPageSize={setPageSize}
                setPageToken={setPageToken}
                pageSize={pageSize}
                listLeasesResult={listLeasesResult} />
            <CreateLeaseDialog
                open={createLeaseDialogOpen}
                setOpen={setCreateLeaseDialogOpen}
                refetch={listLeasesResult.refetch} />
            <Fab 
                color="primary" 
                aria-label="Add" 
                className={classes.fab}
                onClick={() => setCreateLeaseDialogOpen(!createLeaseDialogOpen)}>
                <AddIcon />
            </Fab>
        </React.Fragment>
    )
}
