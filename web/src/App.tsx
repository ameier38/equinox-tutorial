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
  $leaseId: String!
  $eventPageToken: String!
){
  lease(leaseId: $leaseId){
    totalScheduled
    totalPaid
    amountDue
    leaseStatus
    events(pageToken: $eventPageToken){
      eventId
      eventCreatedTime
      eventEffectiveDate
      eventType
    }
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
query Leases(
  $asAt: String!, 
  $leasePageToken: String!,
){
  leases(
    pageSize: 20, 
    pageToken: $leasePageToken,
    asOfDate: {
      asAt: $asAt
    }
  ){
    leaseId
    userId
    startDate
    maturityDate
    monthlyPaymentAmount
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

interface ListLeasesResponse {
  listLeases: Lease[]
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
      userId: '',
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

  const [asAt, setAsAt] = useState<Date | null>(new Date())

  const handleAsAtChange = (date:Date | null) => {
    setAsAt(date)
  }

  const columns = [
    { title: "Lease ID", field: "leaseId" },
    { title: "User ID", field: "userId" },
    { title: "Start Date", field: "startDate" },
    { title: "Maturity Date", field: "maturityDate" },
    { title: "Monthly Payment Amount", field: "monthlyPaymentAmount" }
  ]

  return (
    <Query<ListLeasesResponse> query={LIST_LEASES} variables={{ asAt }} >
      {({ loading, error, data }) => {
        if (loading) return <LinearProgress />
        if (error) return `Error!: ${error.message}`
        return (
          <div className={classes.tableRoot}>
            <MaterialTable
              options={{
                search: false
              }}
              columns={columns}
              data={data ? data.listLeases : []}
              title="Leases"
              components={{
                Toolbar: props => (
                  <div className={classes.asAtField}>
                    <MuiPickersUtilsProvider utils={DateFnsUtils}>
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
               />
          </div>
        )
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
