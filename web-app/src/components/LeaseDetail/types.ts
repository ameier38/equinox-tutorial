import { LeaseEvent } from '../../generated/graphql'

export type State = {
    shouldReset: boolean,
    asOf: {
        asAt: Date,
        asOn: Date
    },
    leaseEvents: LeaseEvent[],
    leaseEventsPageSize: number,
    leaseEventsPageToken: string,
    schedulePaymentDialogOpen: boolean,
    receivePaymentDialogOpen: boolean
}

export type Event =
    | {type: 'RESET_TOGGLED', reset: boolean}
    | {type: 'LEASE_EVENTS_RESET', leaseEvents: LeaseEvent[]}
    | {type: 'AS_OF_UPDATED', asOf: { asAt?: Date, asOn?: Date } }
    | {type: 'LEASE_EVENTS_PAGE_SIZE_UPDATED', pageSize: number }
    | {type: 'LEASE_EVENTS_PAGE_TOKEN_UPDATED', pageToken: string }
    | {type: 'SCHEDULE_PAYMENT_DIALOG_TOGGLED', open: boolean }
    | {type: 'RECEIVE_PAYMENT_DIALOG_TOGGLED', open: boolean }
