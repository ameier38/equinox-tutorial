import React, { useState } from 'react';
import { gql } from 'apollo-boost'
import { Query, Mutation } from 'react-apollo'
import { makeStyles } from '@material-ui/styles'
import Container from '@material-ui/core/Container'
import AppBar from '@material-ui/core/AppBar'
import Toolbar from '@material-ui/core/Toolbar'
import Typography from '@material-ui/core/Typography'
import LinearProgress from '@material-ui/core/LinearProgress'
import TextField from '@material-ui/core/TextField'
import Button from '@material-ui/core/Button'
import MaterialTable from 'material-table'

const useStyles = makeStyles({
  container: {
    paddingTop: 20
  }
})

const GET_LEASE = gql`
query GetLease($leaseId: String!){
  lease(leaseId: $leaseId){
    amountDue
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
query ListLeases{
  listLeases(pageSize: 20, pageToken: ""){
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
  startDate: string,
  maturityDate: string
  monthlyPaymentAmount: number
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

  const [values, setValues] = React.useState<Lease>({
    leaseId: '',
    userId: '',
    startDate: '',
    maturityDate: '',
    monthlyPaymentAmount: 0
  })

  const handleChange = (name: keyof Lease) => (event: React.ChangeEvent<HTMLInputElement>) => {
    setValues({ ...values, [name]: event.target.value });
  }

  return (
    <Mutation<CreateLeaseResponse,Lease> mutation={CREATE_LEASE}>
      {createLease => (
        <form noValidate autoComplete='off' onSubmit={e => {
          e.preventDefault()
          createLease({variables: values})
        }}>
          <TextField
            required
            id='userId'
            label='User ID'
            value={values.userId}
            onChange={handleChange('userId')}
            margin='normal' />
          <TextField
            required
            id='startDate'
            label='Start Date'
            value={values.startDate}
            onChange={handleChange('startDate')}
            margin='normal' />
          <TextField
            required
            id='maturityDate'
            label='Maturity Date'
            value={values.maturityDate}
            onChange={handleChange('maturityDate')}
            margin='normal' />
          <TextField
            required
            id='monthlyPaymentAmount'
            label='Monthly Payment Amount'
            value={values.monthlyPaymentAmount}
            onChange={handleChange('monthlyPaymentAmount')}
            margin='normal' />
          <Button type="submit">Create Lease</Button>
        </form>
      )}
    </Mutation>
  )

}

const LeaseTable: React.FC = () => {
  const columns = [
    { title: "Lease ID", field: "leaseId" },
    { title: "User ID", field: "userId" },
    { title: "Start Date", field: "startDate" },
    { title: "Maturity Date", field: "maturityDate" },
    { title: "Monthly Payment Amount", field: "monthlyPaymentAmount" }
  ]

  return (
    <>
      <Query<ListLeasesResponse> query={LIST_LEASES} >
        {({ loading, error, data }) => {
          if (loading) return <LinearProgress />
          if (error) return `Error!: ${error.message}`
          return (
            <MaterialTable
              columns={columns}
              data={data ? data.listLeases : []}
              title="Leases"
            />
          )
        }}
      </Query>
    </>
  )
}

const App: React.FC = () => {
  const classes = useStyles()
  return (
    <>
      <AppBar position="static" color="default">
        <Toolbar>
          <Typography variant="h6" color="inherit">
            Equinox Tutorial
          </Typography>
        </Toolbar>
      </AppBar>
      <Container className={classes.container} maxWidth="lg">
        <LeaseTable />
      </Container>
    </>
  )
}

export default App;
