const graphQLProtocol = process.env.REACT_APP_GRAPHQL_PROTOCOL || 'http'
const graphQLHost = process.env.REACT_APP_GRAPHQL_HOST || 'localhost'
const graphQLPort = process.env.REACT_APP_GRAPHQL_PORT || '4000'

export const GraphQLConfig = {
    url: `${graphQLProtocol}://${graphQLHost}:${graphQLPort}`
}
