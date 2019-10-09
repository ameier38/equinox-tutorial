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
import { MutationSchedulePaymentArgs } from '../../generated/graphql'
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

type SchedulePaymentDialogState = {
  scheduledDate: Date | null,
  scheduledAmount: number | string
}

type SchedulePaymentDialogProps = {
  leaseId: string,
  open: boolean,
  setOpen: (open:boolean) => void,
}

const SCHEDULE_PAYMENT = `
mutation SchedulePayment($input: SchedulePaymentInput!){
  schedulePayment(input: $input)
}`

export const SchedulePaymentDialog: React.FC<SchedulePaymentDialogProps> = ({ 
    leaseId,
    open,
    setOpen
}) => {

    const classes = useStyles()
    const [schedulePayment] = useMutation<string,MutationSchedulePaymentArgs>(SCHEDULE_PAYMENT)
    const [values, setValues] = useState<SchedulePaymentDialogState>({
        scheduledDate: moment.utc().toDate(),
        scheduledAmount: ''
    })

    const handleScheduledAmountChange = (e:React.ChangeEvent<HTMLInputElement>) => {
        setValues({...values, scheduledAmount: parseFloat(e.target.value)})
    }

    const handleScheduledDateChange = (date:Date | null) => {
        if (date) {
            setValues({...values, scheduledDate: date})
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
                setOpen(false)
            })
        }
    }

    const handleClose = () => {
        setOpen(false)
    }

    return (
        <Dialog open={open} onClose={handleClose} aria-labelledby="form-dialog-title">
            <DialogTitle id="form-dialog-title">Schedule Payment</DialogTitle>
            <DialogContent>
                <MuiPickersUtilsProvider utils={DateFnsUtils}>
                    <KeyboardDatePicker
                        className={classes.textField}
                        required
                        id='startDate'
                        label='Scheduled Date'
                        format='MM/dd/yyyy'
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
