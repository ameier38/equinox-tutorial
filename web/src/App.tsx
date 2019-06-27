import React from 'react';
import { gql } from 'apollo-boost'
import { Query } from 'react-apollo'
import { makeStyles } from '@material-ui/styles'
import Container from '@material-ui/core/Container'
import AppBar from '@material-ui/core/AppBar'
import Toolbar from '@material-ui/core/Toolbar'
import Typography from '@material-ui/core/Typography'
import LinearProgress from '@material-ui/core/LinearProgress'

const useStyles = makeStyles({
  container: {
    paddingTop: 20
  }
})

const GET_LEASE = gql`
{
  lease(leaseId: "96ba9402201d4c3fb09f2e49c9c99015"){
    lease{
      leaseId
      userId
    }
    amountDue
  }
}
`

interface LeaseData {
  lease: {
    lease: {
      leaseId: string,
      userId: string,
      startDate: string,
      maturityDate: string
    }
    totalScheduled: number,
    totalPaid: number,
    amountDue: number
  }
}

const LeaseDetail: React.FC = () => (
  <Query<LeaseData> query={GET_LEASE}>
    {({ loading, error, data }) => {
      if (loading) return <LinearProgress />
      if (error) return `Error!: ${error.message}`
      return (
        <div>
          <div><span>LeaseId: </span>{data ? data.lease.lease.leaseId : ''}</div>
          <div><span>UserId: </span>{data ? data.lease.lease.userId : ''}</div>
          <div><span>AmountDue: </span>{data ? data.lease.amountDue : ''}</div>
        </div>
      )
    }}
  </Query>
)

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
        <LeaseDetail />
      </Container>
    </>
  )
}

export default App;
