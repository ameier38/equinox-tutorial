const {
    REACT_APP_GRAPHQL_PROTOCOL: graphQLProtocol,
    REACT_APP_GRAPHQL_HOST: graphQLHost,
    REACT_APP_GRAPHQL_PORT: graphQLPort
} = process.env

export const GraphQLConfig = {
    uri: `${graphQLProtocol}://${graphQLHost}:${graphQLPort}`
}
