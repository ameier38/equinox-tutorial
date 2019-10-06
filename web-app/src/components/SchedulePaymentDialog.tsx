import React from 'react'

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
