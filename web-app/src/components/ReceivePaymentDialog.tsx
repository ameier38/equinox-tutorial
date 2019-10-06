import React, { useState } from 'react'
import { Theme } from '@material-ui/core/styles'
import { makeStyles, createStyles } from '@material-ui/styles'
import Dialog from '@material-ui/core/Dialog';
import DialogActions from '@material-ui/core/DialogActions'
import DialogContent from '@material-ui/core/DialogContent'
import DialogTitle from '@material-ui/core/DialogTitle'
import Button from '@material-ui/core/Button'
import TextField from '@material-ui/core/TextField'
import InputAdornment from '@material-ui/core/InputAdornment'
import { MuiPickersUtilsProvider, KeyboardDatePicker } from '@material-ui/pickers'
import DateFnsUtils from '@date-io/date-fns'
import { v4 as uuid } from 'uuid'
import { useMutation, useQuery } from '@apollo/react-hooks'
import gql from 'graphql-tag'
import { AsOfDate } from '../types'

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

const RECEIVE_PAYMENT = gql`
mutation ReceivePayment(
  $leaseId: String!,
  $paymentId: String!,
  $paymentDate: String!,
  $paymentAmount: Float!
){
  receivePayment(
    leaseId: $leaseId,
    paymentId: $paymentId,
    paymentDate: $paymentDate,
    paymentAmount: $paymentAmount
  )
}`

type ReceivePaymentResponse = {
  receivePayment: string
}

const ReceivePaymentDialog: React.FC<ReceivePaymentDialogProps> =
  ({ setAsOfDate, leaseId, open, setOpen }) => {

    const classes = useStyles()
    const [receivePayment, { data }] = useMutation(RECEIVE_PAYMENT)

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
    )
  }
