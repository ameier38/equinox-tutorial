# Equinox Tutorial Web App
React application to manage leases.

## Development
Start services.
```
docker-compose up -d --build web-app
```

The local files are mounted into the container so hot-loading will work.
Navigate to the development site at http://localhost:3000.

To generate test data, change into the `lease-api` directory and run:
```
fake build -t Test
```

## Updating protobuf files.
If you update the the protobuf files, first rebuild and start the GraphQL API,
then run regenerate the types using the following command.
```
npm run generate
```

## Resources
- [create-react-app](https://facebook.github.io/create-react-app/docs/adding-typescript)
- [React Material-UI](https://material-ui.com/)
- [GraphQL Hooks](https://github.com/nearform/graphql-hooks)
- [d3.js Bar Chart Tutorial](https://blog.risingstack.com/d3-js-tutorial-bar-charts-with-javascript/)
- [d3.js Thinking with Joins](https://bost.ocks.org/mike/join/)
