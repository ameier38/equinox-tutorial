import { ApolloClient } from 'apollo-client'
import { HttpLink } from 'apollo-link-http'
import { InMemoryCache } from 'apollo-cache-inmemory'
import { typeDefs, resolvers } from './resolvers'
import * as config from './config'

const cache = new InMemoryCache()

export const client = new ApolloClient({
    link: new HttpLink({ uri: config.GraphQLConfig.uri }),
    cache,
    typeDefs,
    resolvers,
})

cache.writeData({
    data: {
        asAt: new Date(),
        asOn: new Date(),
        createLeaseDialogOpen: false,
        schedulePaymentDialogOpen: false,
        receivePaymentDialogOpen: false
    }
})
