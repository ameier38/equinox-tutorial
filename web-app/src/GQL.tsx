import { gql } from 'apollo-boost'

export const GET_LEASE = gql`
query GetLease(
  $asOn: String!,
  $asAt: String!,
  $leaseId: String!
){
  getLease(
    leaseId: $leaseId,
    asOfDate: {
      asOn: $asOn,
      asAt: $asAt
    }
  ){
    totalScheduled
    totalPaid
    amountDue
    leaseStatus
    listEvents(
      pageSize: 1000,
      pageToken: "",
      asOfDate: {
        asOn: $asOn,
        asAt: $asAt
      }
    ){
      events {
        eventId
        eventCreatedTime
        eventEffectiveDate
        eventType
      }
    }
  }
}`

export const LIST_LEASES = gql`
query ListLeases(
  $asOn: String!,
  $asAt: String!, 
  $leasePageToken: String!,
  $leasePageSize: Int!
){
  listLeases(
    asOfDate: {
      asOn: $asOn,
      asAt: $asAt
    },
    pageToken: $leasePageToken,
    pageSize: $leasePageSize
  ){
    leases {
      leaseId
      userId
      startDate
      maturityDate
      monthlyPaymentAmount
    }
    nextPageToken
    totalCount
  }
}`

export const SCHEDULE_PAYMENT = gql`
mutation SchedulePayment(
  $leaseId: String!,
  $paymentId: String!,
  $paymentDate: String!,
  $paymentAmount: Float!
){
  schedulePayment(
    leaseId: $leaseId,
    paymentId: $paymentId,
    paymentDate: $paymentDate,
    paymentAmount: $paymentAmount
  )
}`

export const DELETE_LEASE_EVENT = gql`
mutation DeleteLeaseEvent(
  $leaseId: String!,
  $eventId: Int!
){
  deleteLeaseEvent(
    leaseId: $leaseId,
    eventId: $eventId
  )
}`

export type CreateLeaseResponse = {
  createLease: string
}

export type PaymentRequest = {
  leaseId: string,
  paymentId: string,
  paymentDate: Date,
  paymentAmount: number
}

export type SchedulePaymentResponse = {
  schedulePayment: string
}

export type DeleteLeaseEventRequest = {
  leaseId: string,
  eventId: number
}

export type DeleteLeaseEventResponse = {
  deleteLeaseEvent: string
}

export interface Lease {
  leaseId: string,
  userId: string,
  startDate: Date,
  maturityDate: Date,
  monthlyPaymentAmount: string
}

export interface LeaseEvent {
  eventId: number,
  eventCreatedTime: Date,
  eventEffectiveDate: Date,
  eventType: string
}

export interface GetLeaseResponse {
  getLease: {
    totalScheduled: number,
    totalPaid: number,
    amountDue: number,
    listEvents: {
      events: LeaseEvent[]
    }
  }
}

export interface ListLeasesResponse {
  listLeases: {
    leases: Lease[],
    prevPageToken: string,
    nextPageToken: string,
    totalCount: number
  }
}
