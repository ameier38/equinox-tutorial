import { Resolvers } from 'apollo-client'
import gql from 'graphql-tag'

export const typeDefs = gql`
    extend type Query {
        asAt: Date
        asOn: Date
        createLeaseDialogOpen: Boolean
        schedulePaymentDialogOpen: Boolean
        receivePaymentDialogOpen: Boolean
    }
`

export const resolvers : Resolvers = {
}