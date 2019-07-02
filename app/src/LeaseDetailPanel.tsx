import React, { useState } from 'react'
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import { Query, Mutation, MutationFn } from 'react-apollo'
import { v4 as uuid } from 'uuid'
import { 
  GET_LEASE, 
  SCHEDULE_PAYMENT,
  RECEIVE_PAYMENT,
  DELETE_LEASE_EVENT,
  GetLeaseResponse, 
  PaymentRequest,
  SchedulePaymentResponse,
  ReceivePaymentResponse,
  DeleteLeaseEventRequest,
  DeleteLeaseEventResponse,
  LeaseEvent 
} from './GQL'
import TextField from '@material-ui/core/TextField'
import DateFnsUtils from '@date-io/date-fns'
import { 
  MuiPickersUtilsProvider, 
  KeyboardDatePicker
} from '@material-ui/pickers'
import grey from '@material-ui/core/colors/grey'
import Grid from '@material-ui/core/Grid'
import InputAdornment from '@material-ui/core/InputAdornment'
import Card from '@material-ui/core/Card'
import CardActions from '@material-ui/core/CardActions'
import CardContent from '@material-ui/core/CardContent'
import Dialog from '@material-ui/core/Dialog';
import DialogActions from '@material-ui/core/DialogActions'
import DialogContent from '@material-ui/core/DialogContent'
import DialogTitle from '@material-ui/core/DialogTitle'
import Button from '@material-ui/core/Button'
import Typography from '@material-ui/core/Typography'
import LinearProgress from '@material-ui/core/LinearProgress'
import MaterialTable from 'material-table'
import { AsOfDate } from './Types'

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      padding: theme.spacing(1)
    },
    textField: {
      marginLeft: theme.spacing(1),
      marginRight: theme.spacing(1),
    },
    lightPaper: {
      backgroundColor: theme.palette.grey[600]
    }
  })
)

const moneyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
})

type SchedulePaymentDialogState = {
  scheduleDate: Date | null,
  scheduleAmount: number | string
}

type SchedulePaymentDialogProps = {
  setAsOfDate: (asOf:AsOfDate) => void,
  leaseId: string,
  open: boolean,
  setOpen: (open: boolean) => void
}

const SchedulePaymentDialog: React.FC<SchedulePaymentDialogProps> =
  ({ setAsOfDate, leaseId, open, setOpen }) => {

    const classes = useStyles()

    const [values, setValues] = useState<SchedulePaymentDialogState>({
      scheduleDate: new Date(),
      scheduleAmount: ''
    })

    const handleScheduleAmountChange = (e:React.ChangeEvent<HTMLInputElement>) => {
      setValues({...values, scheduleAmount: parseFloat(e.target.value)})
    }

    const handleScheduleDateChange = (date:Date | null) => {
      if (date) {
        setValues({...values, scheduleDate: date})
      }
    }

    const handleSubmit = (schedulePayment:MutationFn<SchedulePaymentResponse,PaymentRequest>) => 
      () => {
        if (values.scheduleDate && typeof values.scheduleAmount !== 'string') {
          schedulePayment({
            variables: {
              leaseId,
              paymentId: uuid(),
              paymentDate: values.scheduleDate,
              paymentAmount: values.scheduleAmount
            }
          }).then(() => {
            setOpen(false)
            let newDate = new Date()
            newDate.setSeconds(newDate.getSeconds() + 10)
            setAsOfDate({
              asAt: newDate,
              asOn: newDate
            })
          })
        }
      }

    const handleClose = () => {
      setOpen(false)
    }

    return (
      <Mutation<SchedulePaymentResponse,PaymentRequest>
        mutation={SCHEDULE_PAYMENT} >
        {schedulePayment => (
          <Dialog open={open} onClose={handleClose} aria-labelledby="form-dialog-title">
            <DialogTitle id="form-dialog-title">Schedule Payment</DialogTitle>
            <DialogContent>
              <MuiPickersUtilsProvider utils={DateFnsUtils}>
                <KeyboardDatePicker
                  className={classes.textField}
                  required
                  id='startDate'
                  label='Payment Date'
                  format='MM/dd/yyyy'
                  value={values.scheduleDate}
                  onChange={handleScheduleDateChange}
                  margin='normal' />
              </MuiPickersUtilsProvider>
              <TextField
                type='number'
                className={classes.textField}
                margin='normal'
                label="Schedule Amount"
                value={values.scheduleAmount}
                onChange={handleScheduleAmountChange}
                InputProps={{
                  startAdornment: <InputAdornment position="start">$</InputAdornment>,
                }} />
            </DialogContent>
            <DialogActions>
              <Button 
                onClick={handleClose} 
                color="primary">
                Cancel
              </Button>
              <Button
                variant='contained'
                color='primary'
                onClick={handleSubmit(schedulePayment)}>
                Schedule Payment
              </Button>
            </DialogActions>
          </Dialog>
        )}
      </Mutation>
    )
  }

type TotalScheduledCardProps = {
  setAsOfDate: (asOf:AsOfDate) => void,
  leaseId: string,
  totalScheduled: number
}

const TotalScheduledCard: React.FC<TotalScheduledCardProps> = 
  ({ setAsOfDate, leaseId, totalScheduled }) => {
    const classes = useStyles()
    const [dialogOpen, setDialogOpen] = useState(false)

    return (
      <>
        <Card className={classes.lightPaper}>
          <CardContent>
            <Typography gutterBottom variant='h6'>
              Total Scheduled: {moneyFormatter.format(totalScheduled)}
            </Typography>
          </CardContent>
          <CardActions>
            <Button
              color='primary'
              onClick={() => setDialogOpen(true)}>
              Schedule Payment
            </Button>
          </CardActions>
        </Card>
        <SchedulePaymentDialog
          setAsOfDate={setAsOfDate}
          leaseId={leaseId}
          open={dialogOpen}
          setOpen={setDialogOpen} />
      </>
    )
  }

type ReceivePaymentDialogState = {
  paymentDate: Date | null,
  paymentAmount: number | string
}

type ReceivePaymentDialogProps = {
  setAsOfDate: (asOf:AsOfDate) => void,
  leaseId: string,
  open: boolean,
  setOpen: (open: boolean) => void
}

const ReceivePaymentDialog: React.FC<ReceivePaymentDialogProps> =
  ({ setAsOfDate, leaseId, open, setOpen }) => {

    const classes = useStyles()

    const [values, setValues] = useState<ReceivePaymentDialogState>({
      paymentDate: new Date(),
      paymentAmount: ''
    })

    const handlePaymentAmountChange = (e:React.ChangeEvent<HTMLInputElement>) => {
      setValues({...values, paymentAmount: parseFloat(e.target.value)})
    }

    const handlePaymentDateChange = (date:Date | null) => {
      if (date) {
        setValues({...values, paymentDate: date})
      }
    }

    const handleSubmit = (receivePayment:MutationFn<ReceivePaymentResponse,PaymentRequest>) => 
      () => {
        if (values.paymentDate && typeof values.paymentAmount !== 'string') {
          receivePayment({
            variables: {
              leaseId,
              paymentId: uuid(),
              paymentDate: values.paymentDate,
              paymentAmount: values.paymentAmount
            }
          }).then(() => {
            setOpen(false)
            let newDate = new Date()
            newDate.setSeconds(newDate.getSeconds() + 10)
            setAsOfDate({
              asAt: newDate,
              asOn: newDate
            })
          })
        }
      }

    const handleClose = () => {
      setOpen(false)
    }

    return (
      <Mutation<ReceivePaymentResponse,PaymentRequest>
        mutation={RECEIVE_PAYMENT} >
        {receivePayment => (
          <Dialog open={open} onClose={handleClose} aria-labelledby="form-dialog-title">
            <DialogTitle id="form-dialog-title">Receive Payment</DialogTitle>
            <DialogContent>
              <MuiPickersUtilsProvider utils={DateFnsUtils}>
                <KeyboardDatePicker
                  className={classes.textField}
                  required
                  id='startDate'
                  label='Payment Date'
                  format='MM/dd/yyyy'
                  value={values.paymentDate}
                  onChange={handlePaymentDateChange}
                  margin='normal' />
              </MuiPickersUtilsProvider>
              <TextField
                className={classes.textField}
                margin='normal'
                label="Payment Amount"
                value={values.paymentAmount}
                onChange={handlePaymentAmountChange}
                InputProps={{
                  startAdornment: <InputAdornment position="start">$</InputAdornment>,
                }} />
            </DialogContent>
            <DialogActions>
              <Button 
                onClick={handleClose} 
                color="primary">
                Cancel
              </Button>
              <Button
                variant='contained'
                color='primary'
                onClick={handleSubmit(receivePayment)}>
                Receive Payment
              </Button>
            </DialogActions>
          </Dialog>
        )}
      </Mutation>
    )
  }

type TotalPaidCardProps = {
  setAsOfDate: (asOf:AsOfDate) => void,
  leaseId: string,
  totalPaid: number
}

const TotalPaidCard: React.FC<TotalPaidCardProps> = 
  ({ setAsOfDate, leaseId, totalPaid }) => {
    const classes = useStyles()
    const [dialogOpen, setDialogOpen] = useState(false)

    return (
      <>
        <Card className={classes.lightPaper}>
          <CardContent>
            <Typography gutterBottom variant='h6'>
              Total Paid: {moneyFormatter.format(totalPaid)}
            </Typography>
          </CardContent>
          <CardActions>
            <Button
              color='primary'
              onClick={() => setDialogOpen(true)}>
              Receive Payment
            </Button>
          </CardActions>
        </Card>
        <ReceivePaymentDialog
          setAsOfDate={setAsOfDate}
          leaseId={leaseId}
          open={dialogOpen}
          setOpen={setDialogOpen} />
      </>
    )
  }

type LeaseEventTableProps = {
  leaseId: string,
  leaseEvents: LeaseEvent[]
}

const LeaseEventTable: React.FC<LeaseEventTableProps> =
  ({ leaseId, leaseEvents }) => {

    const columns = [
        { title: "Event ID", field: "eventId"},
        { title: "Event Created Time", field: "eventCreatedTime"},
        { title: "Event Effective Date", field: "eventEffectiveDate"},
        { title: "Event Type", field: "eventType"}
    ]

    return (
      <Mutation<DeleteLeaseEventResponse,DeleteLeaseEventRequest>
        mutation={DELETE_LEASE_EVENT}>
        { deleteLeaseEvent => (
          <MaterialTable
            title="Lease Events"
            style={{
              backgroundColor: grey[600],
              padding: 10,
            }}
            columns={columns}
            data={leaseEvents}
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
                      leaseId,
                      eventId: row.eventId
                    }
                  }).then()
            }} />
        )}
      </Mutation>
    )
  }

type LeaseDetailPanelProps = {
    setAsOfDate: (asOf:AsOfDate) => void,
    asOfDate: AsOfDate,
    leaseId: string
}

const LeaseDetailPanel: React.FC<LeaseDetailPanelProps> = 
  ({ setAsOfDate, asOfDate, leaseId }) => {

    const classes = useStyles()

    return (
      <Query<GetLeaseResponse> 
          query={GET_LEASE}
          variables={{
            asOn: asOfDate.asOn,
            asAt: asOfDate.asAt,
            leaseId: leaseId
          }}>
          {({ loading, error, data }) => {
            if (loading) return <LinearProgress />
            if (error) return `Error!: ${error.message}`
            if (data) {
              return(
                <div className={classes.root}>
                  <Grid container spacing={2}>
                    <Grid item xs={6} md={3}>
                      <TotalScheduledCard
                        setAsOfDate={setAsOfDate}
                        leaseId={leaseId}
                        totalScheduled={data.getLease.totalScheduled} />
                    </Grid>
                    <Grid item xs={6} md={3}>
                      <TotalPaidCard
                        setAsOfDate={setAsOfDate}
                        leaseId={leaseId}
                        totalPaid={data.getLease.totalPaid} />
                    </Grid>
                    <Grid item xs={12}>
                      <LeaseEventTable 
                        leaseId={leaseId} 
                        leaseEvents={data.getLease.listEvents.events} />
                    </Grid>
                  </Grid>
                </div>
              )
            }          
          }}
      </Query>
    )
  }

export default LeaseDetailPanel
