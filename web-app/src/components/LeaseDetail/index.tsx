import React, { useState } from 'react'
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import { 
  GET_LEASE, 
  SCHEDULE_PAYMENT,
  RECEIVE_PAYMENT,
  DELETE_LEASE_EVENT,
  GetLeaseResponse, 
  PaymentRequest,
  SchedulePaymentResponse,
  ReceivePaymentResponse,
  DeleteLeaseEventRequest,
  DeleteLeaseEventResponse,
  LeaseEvent 
} from './GQL'
import grey from '@material-ui/core/colors/grey'
import Grid from '@material-ui/core/Grid'
import Card from '@material-ui/core/Card'
import CardActions from '@material-ui/core/CardActions'
import CardContent from '@material-ui/core/CardContent'
import LinearProgress from '@material-ui/core/LinearProgress'
import MaterialTable from 'material-table'
import { AsOfDate } from './Types'

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

const moneyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
})

type LeaseDetailPanelProps = {
    setAsOfDate: (asOf:AsOfDate) => void,
    asOfDate: AsOfDate,
    leaseId: string
}

const LeaseDetailPanel: React.FC<LeaseDetailPanelProps> = 
  ({ setAsOfDate, asOfDate, leaseId }) => {

    const classes = useStyles()

    return (
      <Query<GetLeaseResponse> 
          query={GET_LEASE}
          variables={{
            asOn: asOfDate.asOn,
            asAt: asOfDate.asAt,
            leaseId: leaseId
          }}>
          {({ loading, error, data }) => {
            if (loading) return <LinearProgress />
            if (error) return `Error!: ${error.message}`
            if (data) {
              return(
                <div className={classes.root}>
                  <Grid container spacing={2}>
                    <Grid item xs={6} md={3}>
                      <TotalScheduledCard
                        setAsOfDate={setAsOfDate}
                        leaseId={leaseId}
                        totalScheduled={data.getLease.totalScheduled} />
                    </Grid>
                    <Grid item xs={6} md={3}>
                      <TotalPaidCard
                        setAsOfDate={setAsOfDate}
                        leaseId={leaseId}
                        totalPaid={data.getLease.totalPaid} />
                    </Grid>
                    <Grid item xs={12}>
                      <LeaseEventTable 
                        leaseId={leaseId} 
                        leaseEvents={data.getLease.listEvents.events} />
                    </Grid>
                  </Grid>
                </div>
              )
            }          
          }}
      </Query>
    )
  }

export default LeaseDetailPanel
