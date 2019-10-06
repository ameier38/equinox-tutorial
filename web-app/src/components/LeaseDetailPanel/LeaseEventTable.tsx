import React from 'react'

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
            style={{
              backgroundColor: grey[600],
              padding: 10,
            }}
            columns={columns}
            data={leaseEvents}
            options={{
              search: false,
              headerStyle: {
                backgroundColor: grey[600],
              },
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
