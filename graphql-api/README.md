# GraphQL API
Main client facing API which stiches together the different gRPC APIs.

## Setup
Install Node Version Manager.
```
scoop install nvm
```

Install Node.
```
nvm install 10.16.0
```
> You can run `nvm list available` to see all available version.

Install GraphQL CLI.
```
npm install -g graphql-cli
```

## Usage
Start Event Store and the Lease API.
```
docker-compose up -d eventstore
docker-compose up -d lease-api
```

Start the GraphQL API.
```
fake build -t Serve
```

Start the GraphQL Playground.
```
graphql playground
```
> This command will start the GraphQL Playground server
and read the `.graphqlconfig.yaml` in this repository.

## Development
Install the .NET SDK.
```
scoop install dotnet-sdk
```

Create a `.graphqlconfig.yaml` file.
```
graphql init
```
> Follow the prompts to populate the values.

## Resources
- [GraphQL](https://graphql.org/)
- [GraphQL CLI](https://github.com/graphql-cli/graphql-cli)
- [Designing Graphql Mutations](https://blog.apollographql.com/designing-graphql-mutations-e09de826ed97)
