import React, { useState } from 'react'
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import { Query, Mutation, MutationFn } from 'react-apollo'
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
import LinearProgress from '@material-ui/core/LinearProgress'
import MaterialTable from 'material-table'

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      paddingLeft: theme.spacing(10),
      paddingRight: theme.spacing(10)
    }
  })
)

type LeaseStateTableProps = {
  getLeaseResponse: GetLeaseResponse
}

const LeaseStateTable: React.FC<LeaseStateTableProps> = 
  ({ getLeaseResponse }) => {

    const columns = [
        { title: "Total Scheduled", field: "totalScheduled"},
        { title: "Total Paid", field: "totalPaid"},
        { title: "Amount Due", field: "amountDue"},
        { title: "Status", field: "leaseStatus"}
    ]

    return (
      <MaterialTable
        options={{
          showTitle: false,
          paging: false,
          search: false
        }}
        columns={columns}
        data={[getLeaseResponse.getLease]} />
    )
  }

type LeaseEventTableProps = {
  leaseId: string,
  leaseEvents: LeaseEvent[]
}

const LeaseEventTable: React.FC<LeaseEventTableProps> =
  ({ leaseId, leaseEvents }) => {

    const columns = [
        { title: "Event ID", field: "eventId"},
        { title: "Event Created Time", field: "eventCreatedTime"},
        { title: "Event Effective Date", field: "eventEffectiveDate"},
        { title: "Event Type", field: "eventType"}
    ]

    return (
      <Mutation<DeleteLeaseEventResponse,DeleteLeaseEventRequest>
        mutation={DELETE_LEASE_EVENT}>
        { deleteLeaseEvent => (
          <MaterialTable
            title="Lease Events"
            columns={columns}
            data={leaseEvents}
            options={{
              search: false
            }}
            editable={{
              onRowDelete: row =>
                  deleteLeaseEvent({
                    variables: {
                      leaseId,
                      eventId: row.eventId
                    }
                  }).then()
            }} />
        )}
      </Mutation>
    )
  }

type LeaseDetailPanelProps = {
    asOn: Date,
    asAt: Date,
    leaseId: string
}

const LeaseDetailPanel: React.FC<LeaseDetailPanelProps> = 
  ({ asOn, asAt, leaseId }) => {

    const classes = useStyles()

    return (
      <Query<GetLeaseResponse> 
          query={GET_LEASE}
          variables={{
            asOn,
            asAt,
            leaseId: leaseId
          }}>
          {({ loading, error, data }) => {
            if (loading) return <LinearProgress />
            if (error) return `Error!: ${error.message}`
            if (data) (
              <div className={classes.root}>
                <LeaseStateTable 
                  getLeaseResponse={data} /> 
                <LeaseEventTable 
                  leaseId={leaseId} 
                  leaseEvents={data.getLease.listEvents.events} />
              </div>
            )
          }}
      </Query>
    )
  }

export default LeaseDetailPanel
