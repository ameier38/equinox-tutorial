import React, { useState } from 'react'
import { Theme } from '@material-ui/core/styles'
import { makeStyles, createStyles } from '@material-ui/styles'
import moment from 'moment'
import {
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Button,
    TextField,
    InputAdornment
} from '@material-ui/core';
import { MuiPickersUtilsProvider, KeyboardDatePicker } from '@material-ui/pickers'
import MomentUtils from '@date-io/moment'
import { v4 as uuid } from 'uuid'
import { useMutation } from 'graphql-hooks'
import { MutationReceivePaymentArgs } from '../../generated/graphql'
import { Event } from './types'

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
  dispatch: React.Dispatch<Event>
}

const RECEIVE_PAYMENT = `
mutation ReceivePayment($input: ReceivePaymentInput!){
  receivePayment(input: $input)
}`

export const ReceivePaymentDialog: React.FC<ReceivePaymentDialogProps> = ({ 
    leaseId,
    open,
    dispatch
}) => {

    const classes = useStyles()
    const [receivePayment] = useMutation<string,MutationReceivePaymentArgs>(RECEIVE_PAYMENT)
    const [values, setValues] = useState<ReceivePaymentDialogState>({
        receivedDate: moment.utc().toDate(),
        receivedAmount: ''
    })

    const reset = () => {
        setValues({
            receivedDate: moment.utc().toDate(),
            receivedAmount: ''
        })
    }

    const handleReceivedAmountChange = (e:React.ChangeEvent<HTMLInputElement>) => {
        setValues({...values, receivedAmount: parseFloat(e.target.value)})
    }

    const handleReceivedDateChange = (date:moment.Moment | null) => {
        if (date) {
            setValues({...values, receivedDate: date.toDate()})
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
                const now = moment.utc().add(10, 'seconds').toDate()
                reset()
                dispatch({type: 'RECEIVE_PAYMENT_DIALOG_TOGGLED', open: false})
                dispatch({type: 'RESET_TOGGLED', reset: true})
                dispatch({type: 'AS_OF_UPDATED', asOf: { asAt: now, asOn: now}})
            })
        }
      }

    const handleClose = () => {
        dispatch({type: 'RECEIVE_PAYMENT_DIALOG_TOGGLED', open: false})
    }

    return (
        <Dialog open={open} onClose={handleClose} aria-labelledby="form-dialog-title">
            <DialogTitle id="form-dialog-title">Receive Payment</DialogTitle>
            <DialogContent>
                <MuiPickersUtilsProvider utils={MomentUtils}>
                    <KeyboardDatePicker
                    className={classes.textField}
                    required
                    id='startDate'
                    label='Received Date'
                    format='MM/DD/YYYY'
                    value={values.receivedDate}
                    onChange={handleReceivedDateChange}
                    margin='normal' />
                </MuiPickersUtilsProvider>
                <TextField
                    type='number'
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
