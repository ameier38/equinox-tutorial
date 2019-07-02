import React, { useState } from 'react';
import { Query } from 'react-apollo'
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import LinearProgress from '@material-ui/core/LinearProgress'
import MaterialTable from 'material-table'
import DateFnsUtils from '@date-io/date-fns'
import { 
  MuiPickersUtilsProvider, 
  KeyboardDateTimePicker,
} from '@material-ui/pickers'
import LeaseDetailPanel from './LeaseDetailPanel'
import { LIST_LEASES, ListLeasesResponse, Lease } from './GQL'

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    container: {
      paddingTop: 20,
    },
    tableRoot: {
      margin: 10,
    },
    asAtField: {
      marginLeft: 20,
    },
  })
)

const LeaseTable: React.FC = () => {
  const classes = useStyles();

  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [pageToken, setPageToken] = useState("")
  const [prevPageToken, setPrevPageToken] = useState("")
  const [nextPageToken, setNextPageToken] = useState("")
  const [asOn, setAsOn] = useState<Date>(new Date())
  const [asAt, setAsAt] = useState<Date>(new Date())

  const handleAsOnChange = (date:Date | null) => {
    if (date) {
      setAsOn(date)
    }
  }

  const handleAsAtChange = (date:Date | null) => {
    if (date) {
      setAsAt(date)
    }
  }

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

  const columns = [
    { title: "Lease ID", field: "leaseId" },
    { title: "User ID", field: "userId" },
    { title: "Start Date", field: "startDate" },
    { title: "Maturity Date", field: "maturityDate" },
    { title: "Monthly Payment Amount", field: "monthlyPaymentAmount" }
  ]

  return (
    <Query<ListLeasesResponse> 
      query={LIST_LEASES} 
      variables={{ 
        asOn,
        asAt,
        leasePageToken: pageToken,
        leasePageSize: pageSize
      }} >
      {({ loading, error, data }) => {
        if (loading) return <LinearProgress />
        if (error) return `Error!: ${error.message}`
        if (data) {
          setPrevPageToken(data.listLeases.prevPageToken)
          setNextPageToken(data.listLeases.nextPageToken)
          return (
            <div className={classes.tableRoot}>
              <MaterialTable
                options={{
                  search: false
                }}
                columns={columns}
                data={data.listLeases.leases}
                onChangePage={handleChangePage}
                onChangeRowsPerPage={handleChangeRowsPerPage}
                title="Leases"
                components={{
                  Toolbar: props => (
                    <div className={classes.asAtField}>
                      <MuiPickersUtilsProvider utils={DateFnsUtils}>
                        <KeyboardDateTimePicker
                          required
                          id='asOnDate'
                          label='As On Date'
                          value={asOn}
                          onChange={handleAsOnChange}
                          margin='normal' />
                        <KeyboardDateTimePicker
                          required
                          id='asAtDate'
                          label='As At Date'
                          value={asAt}
                          onChange={handleAsAtChange}
                          margin='normal' />
                      </MuiPickersUtilsProvider>
                    </div>
                  ),
                }}
                detailPanel={(rowData:Lease) => (
                  <LeaseDetailPanel
                    asOn={asOn}
                    asAt={asAt}
                    leaseId={rowData.leaseId} />
                )}
                />
            </div>
          )
        }
      }}
    </Query>
  )
}

export default LeaseTable
