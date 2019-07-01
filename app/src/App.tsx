import React, { useState } from 'react';
import { gql } from 'apollo-boost'
import { Query, Mutation, MutationFn } from 'react-apollo'
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import { v4 as uuid } from 'uuid'
import Container from '@material-ui/core/Container'
import Paper from '@material-ui/core/Paper'
import Grid from '@material-ui/core/Grid'
import AppBar from '@material-ui/core/AppBar'
import Toolbar from '@material-ui/core/Toolbar'
import Typography from '@material-ui/core/Typography'
import LinearProgress from '@material-ui/core/LinearProgress'
import TextField from '@material-ui/core/TextField'
import Button from '@material-ui/core/Button'
import MaterialTable from 'material-table'
import DateFnsUtils from '@date-io/date-fns'
import { 
  MuiPickersUtilsProvider, 
  KeyboardDatePicker,
  KeyboardDateTimePicker,
} from '@material-ui/pickers'

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    container: {
      paddingTop: 20,
    },
    tableRoot: {
      margin: 10,
    },
    formRoot: {
      margin: 10,
    },
    formHeader: {
      paddingLeft: 10,
    },
    formPaper: {
      flexGrow: 1,
      padding: 10,
    },
    formButton: {
      alignSelf: 'flex-end',
    },
    textField: {
      marginLeft: theme.spacing(1),
      marginRight: theme.spacing(1),
    },
    asAtField: {
      marginLeft: 20,
    }
  })
)

const GET_LEASE = gql`
query GetLease(
  $asOn: String!,
  $asAt: String!,
  $leaseId: String!
){
  getLease(
    leaseId: $leaseId,
    asOfDate: {
      asOn: $asOn,
      asAt: $asAt
    }
  ){
    totalScheduled
    totalPaid
    amountDue
    leaseStatus
  }
}
`

const CREATE_LEASE = gql`
mutation CreateLease(
  $leaseId: String!
  $userId: String!,
  $startDate: String!,
  $maturityDate: String!,
  $monthlyPaymentAmount: Float!
){
  createLease(
    leaseId: $leaseId,
    userId: $userId,
    startDate: $startDate,
    maturityDate: $maturityDate,
    monthlyPaymentAmount: $monthlyPaymentAmount
  ){
    leaseId
    message
  }
}
`

const LIST_LEASES = gql`
query ListLeases(
  $asOn: String!,
  $asAt: String!, 
  $leasePageToken: String!,
  $leasePageSize: Int!
){
  listLeases(
    asOfDate: {
      asOn: $asOn,
      asAt: $asAt
    },
    pageToken: $leasePageToken,
    pageSize: $leasePageSize
  ){
    leases {
      leaseId
      userId
      startDate
      maturityDate
      monthlyPaymentAmount
    }
    nextPageToken
    totalCount
  }
}
`

interface Lease {
  leaseId: string,
  userId: string,
  startDate: Date,
  maturityDate: Date,
  monthlyPaymentAmount: string
}

interface LeaseEvent {
  eventId: number,
  eventCreatedTime: Date,
  eventEffectiveDate: Date,
  eventType: string
}

interface GetLeaseResponse {
  getLease: {
    totalScheduled: number,
    totalPaid: number,
    amountDue: number,
    listEvents: {
      events: LeaseEvent[],
      prevPageToken: string,
      nextPageToken: string,
      totalCount: number
    }
  }
}

interface ListLeasesResponse {
  listLeases: {
    leases: Lease[],
    prevPageToken: string,
    nextPageToken: string,
    totalCount: number
  }
}

interface CreateLeaseResponse {
  createLease: {
    leaseId: string,
    message: string
  }
}

const LeaseForm: React.FC = () => {

  const addMonths = (d:Date, months:number) => new Date(d.setMonth(d.getMonth() + months))

  const classes = useStyles()

  const initState = () => {
    return {
      leaseId: uuid(),
      userId: uuid(),
      startDate: new Date(),
      maturityDate: addMonths(new Date(), 12),
      monthlyPaymentAmount: ''
    }
  }

  const [values, setValues] = useState<Lease>(initState())

  const handleChange = (name: keyof Lease) => (event: React.ChangeEvent<HTMLInputElement>) => {
    setValues({ ...values, [name]: event.target.value });
  }

  const handleDateChange = (name: keyof Lease) => (date: Date | null) => {
    setValues({ ...values, [name]: date })
  }

  const handleSubmit = (createLease:MutationFn<CreateLeaseResponse,Lease>) => 
    (e:React.FormEvent<HTMLFormElement>) => {
      e.preventDefault()
      createLease({variables: values})
      setValues(initState())
    }

  return (
    <Mutation<CreateLeaseResponse,Lease> mutation={CREATE_LEASE}>
      {createLease => (
        <div className={classes.formRoot}>
          <form noValidate autoComplete='off' onSubmit={handleSubmit(createLease)}>
            <Paper className={classes.formPaper}>
              <Typography className={classes.formHeader} variant='h6'>
                Create a new lease
              </Typography>
              <Grid container>
                <MuiPickersUtilsProvider utils={DateFnsUtils}>
                  <Grid item xs={12} sm={6} md={3}>
                    <TextField
                      className={classes.textField}
                      required
                      id='userId'
                      label='User ID'
                      value={values.userId}
                      onChange={handleChange('userId')}
                      margin='normal' />
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <KeyboardDatePicker
                      className={classes.textField}
                      required
                      id='startDate'
                      label='Start Date'
                      format='MM/dd/yyyy'
                      value={values.startDate}
                      onChange={handleDateChange('startDate')}
                      margin='normal' />
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <KeyboardDatePicker
                      className={classes.textField}
                      required
                      id='maturityDate'
                      label='Maturity Date'
                      format='MM/dd/yyyy'
                      value={values.maturityDate}
                      onChange={handleDateChange('maturityDate')}
                      margin='normal' />
                  </Grid>
                  <Grid item xs={12} sm={6} md={3}>
                    <TextField
                      className={classes.textField}
                      required
                      id='monthlyPaymentAmount'
                      type='number'
                      label='Payment Amount'
                      value={values.monthlyPaymentAmount}
                      onChange={handleChange('monthlyPaymentAmount')}
                      margin='normal' />
                  </Grid>
                  <Grid container item justify='space-between' xs={12}>
                    <Grid item ></Grid>
                    <Grid item>
                      <Button 
                        variant="contained" 
                        type="submit">
                        Create Lease
                      </Button>
                    </Grid>
                  </Grid>
                </MuiPickersUtilsProvider>
              </Grid>
            </Paper>
          </form>
        </div>
      )}
    </Mutation>
  )
}

const LeaseTable: React.FC = () => {
  const classes = useStyles();

  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [pageToken, setPageToken] = useState("")
  const [prevPageToken, setPrevPageToken] = useState("")
  const [nextPageToken, setNextPageToken] = useState("")
  const [asOn, setAsOn] = useState<Date | null>(new Date())
  const [asAt, setAsAt] = useState<Date | null>(new Date())

  const handleAsOnChange = (date:Date | null) => {
    setAsOn(date)
  }

  const handleAsAtChange = (date:Date | null) => {
    setAsAt(date)
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

  const detailColumns = [
    { title: "Total Scheduled", field: "totalScheduled"},
    { title: "Total Paid", field: "totalPaid"},
    { title: "Amount Due", field: "amountDue"},
    { title: "Status", field: "leaseStatus"}
  ]

  const eventColumns = [
    { title: "Event ID", field: "eventId"},
    { title: "Event Created Time", field: "eventCreatedTime"},
    { title: "Event Effective Date", field: "eventEffectiveDate"},
    { title: "Event Type", field: "eventType"}
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
                detailPanel={rowData => {
                  return (
                    <Query<GetLeaseResponse> 
                      query={GET_LEASE}
                      variables={{
                        asOn,
                        asAt,
                        leaseId: rowData.leaseId
                      }}>
                      {({ loading: detailLoading, error: detailError, data: detailData }) => {
                        if (detailLoading) return <LinearProgress />
                        if (detailError) return `Error!: ${detailError.message}`
                        return (
                          <MaterialTable 
                            options={{
                              search: false
                            }}
                            columns={detailColumns}
                            data={detailData ? [detailData.getLease] : [] }
                            />
                        )
                      }}
                    </Query>
                  )
                }}
                />
            </div>
          )
        }
      }}
    </Query>
  )
}

const App: React.FC = () => {
  const classes = useStyles()
  return (
    <>
      <AppBar position="static" color="primary">
        <Toolbar>
          <Typography variant="h6" color="inherit">
            Equinox Tutorial
          </Typography>
        </Toolbar>
      </AppBar>
      <Container className={classes.container} maxWidth="lg">
        <LeaseForm />
        <LeaseTable />
      </Container>
    </>
  )
}

export default App;
