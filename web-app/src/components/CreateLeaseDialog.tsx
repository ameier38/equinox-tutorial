import React, { useState } from 'react'
import gql from 'graphql-tag'
import { useMutation, useQuery, useApolloClient } from '@apollo/react-hooks'
import { Theme } from '@material-ui/core/styles'
import { makeStyles, createStyles } from '@material-ui/styles'
import { v4 as uuid } from 'uuid'
import Dialog from '@material-ui/core/Dialog'
import DialogActions from '@material-ui/core/DialogActions'
import DialogContent from '@material-ui/core/DialogContent'
import DialogTitle from '@material-ui/core/DialogTitle'
import Grid from '@material-ui/core/Grid'
import TextField from '@material-ui/core/TextField'
import Button from '@material-ui/core/Button'
import DateFnsUtils from '@date-io/date-fns'
import { 
  MuiPickersUtilsProvider, 
  KeyboardDatePicker
} from '@material-ui/pickers'
import { 
    Lease,
    MutationCreateLeaseArgs 
} from '../generated/graphql';

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    textField: {
      marginLeft: theme.spacing(1),
      marginRight: theme.spacing(1),
    },
  })
)

const CREATE_LEASE = gql`
mutation CreateLease($input: CreateLeaseInput!){
  createLease(input: $input)
}`

const CREATE_LEASE_DIALOG_OPEN = gql`
query CreateLeaseDialogOpen {
    createLeaseDialogOpen @client
}
`

export const CreateLeaseDialog: React.FC = () => {

    const addMonths = (d:Date, months:number) => 
      new Date(d.setMonth(d.getMonth() + months))

    const initState = () => ({
        leaseId: uuid(),
        userId: uuid(),
        startDate: new Date(),
        maturityDate: addMonths(new Date(), 12),
        monthlyPaymentAmount: 0
    })

    const classes = useStyles()
    const client = useApolloClient()
    const [values, setValues] = useState<Lease>(initState())
    const [createLease] = useMutation<string, MutationCreateLeaseArgs>(CREATE_LEASE)
    const { data } = useQuery(CREATE_LEASE_DIALOG_OPEN)

    const handleChange = (name: keyof Lease) => 
      (event: React.ChangeEvent<HTMLInputElement>) => {
        setValues({ ...values, [name]: event.target.value });
      }

    const handleDateChange = (name: keyof Lease) => 
      (date: Date | null) => {
        setValues({ ...values, [name]: date })
      }

    const handleClose = () => {
        client.writeData({ data: { createLeaseDialogOpen: false } })
    }

    const handleSubmit = () => {
        createLease({
            variables: { 
                input: values 
            }
        }).then(() => {
            let newDate = new Date()
            newDate.setSeconds(newDate.getSeconds() + 10)
            client.writeData({
                data: {
                    asAt: newDate,
                    asOn: newDate,
                    createLeaseDialogOpen: false
                }
            })
        })
      }

    return (
          <Dialog open={data.createLeaseDialogOpen} onClose={handleClose} aria-labelledby="form-dialog-title">
            <DialogTitle id="form-dialog-title">Create Lease</DialogTitle>
            <DialogContent>
              <Grid container>
                <MuiPickersUtilsProvider utils={DateFnsUtils}>
                  <Grid item xs={12} md={6}>
                    <TextField
                      className={classes.textField}
                      required
                      id='userId'
                      label='User ID'
                      value={values.userId}
                      onChange={handleChange('userId')}
                      margin='normal' />
                  </Grid>
                  <Grid item sm={12} md={6}>
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
                  <Grid item xs={12} md={6}>
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
                  <Grid item xs={12} md={6}>
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
                </MuiPickersUtilsProvider>
              </Grid>
            </DialogContent>
            <DialogActions>
              <Button onClick={handleClose} color="primary">
                Cancel
              </Button>
              <Button onClick={handleSubmit} color="primary">
                Create
              </Button>
            </DialogActions>
          </Dialog>
    )
  }
