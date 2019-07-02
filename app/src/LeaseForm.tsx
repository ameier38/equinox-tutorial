import React, { useState } from 'react'
import { Mutation, MutationFn } from 'react-apollo'
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import { v4 as uuid } from 'uuid'
import Dialog from '@material-ui/core/Dialog';
import DialogActions from '@material-ui/core/DialogActions';
import DialogContent from '@material-ui/core/DialogContent';
import DialogTitle from '@material-ui/core/DialogTitle'
import Grid from '@material-ui/core/Grid'
import TextField from '@material-ui/core/TextField'
import Button from '@material-ui/core/Button'
import DateFnsUtils from '@date-io/date-fns'
import { 
  MuiPickersUtilsProvider, 
  KeyboardDatePicker
} from '@material-ui/pickers'
import { CREATE_LEASE, Lease, CreateLeaseResponse } from './GQL'

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    textField: {
      marginLeft: theme.spacing(1),
      marginRight: theme.spacing(1),
    },
  })
)

type LeaseFormProps = {
  open: boolean,
  setOpen: (open: boolean) => void
}

const LeaseForm: React.FC<LeaseFormProps> = 
  ({open, setOpen}) => {

    const classes = useStyles()

    const addMonths = (d:Date, months:number) => 
      new Date(d.setMonth(d.getMonth() + months))

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

    const handleChange = (name: keyof Lease) => 
      (event: React.ChangeEvent<HTMLInputElement>) => {
        setValues({ ...values, [name]: event.target.value });
      }

    const handleDateChange = (name: keyof Lease) => 
      (date: Date | null) => {
        setValues({ ...values, [name]: date })
      }

    const handleClose = () => {
      setOpen(false)
    }

    const handleSubmit = (createLease:MutationFn<CreateLeaseResponse,Lease>) => 
      () => {
        createLease({variables: values})
        setValues(initState())
        setOpen(false)
      }

    return (
      <Mutation<CreateLeaseResponse,Lease> mutation={CREATE_LEASE}>
        {createLease => (
          <Dialog open={open} onClose={handleClose} aria-labelledby="form-dialog-title">
            <DialogTitle id="form-dialog-title">Create Lease</DialogTitle>
            <DialogContent>
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
            </DialogContent>
            <DialogActions>
              <Button onClick={handleClose} color="primary">
                Cancel
              </Button>
              <Button onClick={handleSubmit(createLease)} color="primary">
                Create
              </Button>
            </DialogActions>
          </Dialog>
        )}
      </Mutation>
    )
  }

export default LeaseForm
