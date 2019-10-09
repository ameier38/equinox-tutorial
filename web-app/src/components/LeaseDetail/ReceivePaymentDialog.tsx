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
import { useMutation } from 'graphql-hooks'
import { MutationReceivePaymentArgs } from '../../generated/graphql'
import moment from 'moment'

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
  receivedDate: Date | null,
  receivedAmount: number | string
}

type ReceivePaymentDialogProps = {
  leaseId: string,
  open: boolean,
  setOpen: (open:boolean) => void,
}

const RECEIVE_PAYMENT = `
mutation ReceivePayment($input: ReceivePaymentInput!){
  receivePayment(input: $input)
}`

export const ReceivePaymentDialog: React.FC<ReceivePaymentDialogProps> = ({ 
    leaseId,
    open,
    setOpen
}) => {

    const classes = useStyles()
    const [receivePayment] = useMutation<string,MutationReceivePaymentArgs>(RECEIVE_PAYMENT)
    const [values, setValues] = useState<ReceivePaymentDialogState>({
      receivedDate: moment.utc().toDate(),
      receivedAmount: ''
    })

    const handleReceivedAmountChange = (e:React.ChangeEvent<HTMLInputElement>) => {
      setValues({...values, receivedAmount: parseFloat(e.target.value)})
    }

    const handleReceivedDateChange = (date:Date | null) => {
      if (date) {
        setValues({...values, receivedDate: date})
      }
    }

    const handleSubmit = () => {
        if (values.receivedDate && typeof values.receivedAmount !== 'string') {
            receivePayment({
                variables: {
                    input: {
                        leaseId,
                        paymentId: uuid(),
                        receivedDate: values.receivedDate,
                        receivedAmount: values.receivedAmount
                    },
                }
            }).then(() => {
                setOpen(false)
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
              label='Received Date'
              format='MM/dd/yyyy'
              value={values.receivedDate}
              onChange={handleReceivedDateChange}
              margin='normal' />
          </MuiPickersUtilsProvider>
          <TextField
            className={classes.textField}
            margin='normal'
            label="Received Amount"
            value={values.receivedAmount}
            onChange={handleReceivedAmountChange}
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
            onClick={handleSubmit}>
            Receive Payment
          </Button>
        </DialogActions>
      </Dialog>
    )
  }
