import React, { useState } from 'react';
import { Query } from 'react-apollo'
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import LinearProgress from '@material-ui/core/LinearProgress'
import MaterialTable, { MTableToolbar } from 'material-table'
import DateFnsUtils from '@date-io/date-fns'
import { 
  MuiPickersUtilsProvider, 
  KeyboardDateTimePicker,
} from '@material-ui/pickers'
import LeaseDetailPanel from './LeaseDetailPanel'
import { LIST_LEASES, ListLeasesResponse, Lease } from './GQL'
import { AsOfDate } from './Types'

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

type LeaseTableProps = {
  asOfDate: AsOfDate,
  setAsOfDate: (asOf:AsOfDate) => void,
}

const LeaseTable: React.FC<LeaseTableProps> = 
  ({asOfDate, setAsOfDate }) => {
    const classes = useStyles();

    const [page, setPage] = useState(1)
    const [pageSize, setPageSize] = useState(10)
    const [pageToken, setPageToken] = useState("")
    const [prevPageToken, setPrevPageToken] = useState("")
    const [nextPageToken, setNextPageToken] = useState("")

    const handleAsOnChange = (date:Date | null) => {
      if (date) {
        setAsOfDate({...asOfDate, asOn: date})
      }
    }

    const handleAsAtChange = (date:Date | null) => {
      if (date) {
        setAsOfDate({...asOfDate, asAt: date})
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
      { title: "Start Date", field: "startDate" },
      { title: "Maturity Date", field: "maturityDate" },
      { title: "Monthly Payment Amount", field: "monthlyPaymentAmount" },
    ]

    return (
      <Query<ListLeasesResponse> 
        query={LIST_LEASES} 
        variables={{ 
          asOn: asOfDate.asOn,
          asAt: asOfDate.asAt,
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
                  style={{
                    padding: 10,
                  }}
                  columns={columns}
                  data={data.listLeases.leases}
                  onChangePage={handleChangePage}
                  onChangeRowsPerPage={handleChangeRowsPerPage}
                  title="Leases"
                  components={{
                    Toolbar: props => (
                      <>
                        <MTableToolbar {...props} />
                        <MuiPickersUtilsProvider utils={DateFnsUtils}>
                          <KeyboardDateTimePicker
                            className={classes.textField}
                            required
                            id='asOnDate'
                            label='As On Date'
                            value={asOfDate.asOn}
                            onChange={handleAsOnChange}
                            margin='normal' />
                          <KeyboardDateTimePicker
                            className={classes.textField}
                            required
                            id='asAtDate'
                            label='As At Date'
                            value={asOfDate.asAt}
                            onChange={handleAsAtChange}
                            margin='normal' />
                        </MuiPickersUtilsProvider>
                      </>
                    ),
                  }}
                  detailPanel={(rowData:Lease) => (
                    <LeaseDetailPanel
                      asOfDate={asOfDate}
                      setAsOfDate={setAsOfDate}
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
