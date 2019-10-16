import React, { useState } from 'react'
import moment from 'moment'
import { Theme } from '@material-ui/core/styles'
import { makeStyles, createStyles } from '@material-ui/styles'
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Button,
    TextField,
    InputAdornment
} from '@material-ui/core';
import { MuiPickersUtilsProvider, KeyboardDatePicker } from '@material-ui/pickers'
import MomentUtils from '@date-io/moment'
import { v4 as uuid } from 'uuid'
import { useMutation } from 'graphql-hooks'
import { MutationSchedulePaymentArgs } from '../../generated/graphql'
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

type SchedulePaymentDialogState = {
    scheduledDate: Date | null,
    scheduledAmount: number | string
}

type SchedulePaymentDialogProps = {
    leaseId: string,
    open: boolean,
    dispatch: React.Dispatch<Event>
}

const SCHEDULE_PAYMENT = `
mutation SchedulePayment($input: SchedulePaymentInput!){
  schedulePayment(input: $input)
}`

export const SchedulePaymentDialog: React.FC<SchedulePaymentDialogProps> = ({ 
    leaseId,
    open,
    dispatch
}) => {

    const classes = useStyles()
    const [schedulePayment] = useMutation<string,MutationSchedulePaymentArgs>(SCHEDULE_PAYMENT)
    const [values, setValues] = useState<SchedulePaymentDialogState>({
        scheduledDate: moment.utc().toDate(),
        scheduledAmount: ''
    })

    const reset = () => {
        setValues({
            scheduledDate: moment.utc().toDate(),
            scheduledAmount: ''
        })
    }

    const handleScheduledAmountChange = (e:React.ChangeEvent<HTMLInputElement>) => {
        setValues({...values, scheduledAmount: parseFloat(e.target.value)})
    }

    const handleScheduledDateChange = (date:moment.Moment | null) => {
        if (date) {
            setValues({...values, scheduledDate: date.toDate()})
        }
    }

    const handleSubmit = () => {
        if (values.scheduledDate && typeof values.scheduledAmount !== 'string') {
            schedulePayment({
                variables: {
                    input: {
                        leaseId: leaseId,
                        paymentId: uuid(),
                        scheduledDate: values.scheduledDate,
                        scheduledAmount: values.scheduledAmount
                    },
                }
            }).then(() => {
                const now = moment.utc().add(10, 'seconds').toDate()
                reset()
                dispatch({type: 'SCHEDULE_PAYMENT_DIALOG_TOGGLED', open: false})
                dispatch({type: 'RESET_TOGGLED', reset: true})
                dispatch({type: 'AS_OF_UPDATED', asOf: { asAt: now, asOn: now}})
            })
        }
    }

    const handleClose = () => {
        dispatch({type: 'SCHEDULE_PAYMENT_DIALOG_TOGGLED', open: false})
    }

    return (
        <Dialog open={open} onClose={handleClose} aria-labelledby="form-dialog-title">
            <DialogTitle id="form-dialog-title">Schedule Payment</DialogTitle>
            <DialogContent>
                <MuiPickersUtilsProvider utils={MomentUtils}>
                    <KeyboardDatePicker
                        className={classes.textField}
                        required
                        id='startDate'
                        label='Scheduled Date'
                        format='MM/DD/YYYY'
                        value={values.scheduledDate}
                        onChange={handleScheduledDateChange}
                        margin='normal' />
                </MuiPickersUtilsProvider>
                <TextField
                    type='number'
                    className={classes.textField}
                    margin='normal'
                    label="Scheduled Amount"
                    value={values.scheduledAmount}
                    onChange={handleScheduledAmountChange}
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
                    Schedule Payment
                </Button>
            </DialogActions>
        </Dialog>
    )
  }
