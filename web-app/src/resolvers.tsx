import { Resolvers } from 'apollo-client'
import gql from 'graphql-tag'

export const typeDefs = gql`
    extend type Query {
        schedulePaymentDialogOpen: Boolean
        receivePaymentDialogOpen: Boolean
    }
`

export const resolvers : Resolvers = {
}