# GraphQL API
Main client facing API.

## Development

### Setup
1. Install the [.NET SDK](https://andrewmeier.dev/win-dev#dotnet).
2. Install [FAKE](https://andrewmeier.dev/win-dev#fake).
3. Install [Paket](https://andrewmeier.dev/win-dev#paket).
4. Add tools path to `PATH`.
    _Linux/macOS_
    ```shell
    export PATH = "$PATH:$HOME/.dotnet/tools"
    ```
    _Windows Powershell_
    ```powershell
    $env:PATH += ";C:/Users/<user>/.dotnet/tools"
    ```
5. Install [GraphQL Playground](https://github.com/prisma-labs/graphql-playground)
> Used for interactively running queries.

## Testing
Start the server locally for integration tests.
```
docker-compose up -d --build graphql-api
```

Run integration tests.
```
fake build -t TestIntegrations
```

## Development

### Updating protobuf files
Update the protobuf files and generate the new outputs. 
See [proto README](../proto/README.md) for instructions.

Then copy the generated files and build the Proto project.
```
fake build -t UpdateProtos
```

## Resources
- [Suave](https://suave.io/)
- [GraphQL](https://graphql.org/)
- [GraphQL CLI](https://github.com/graphql-cli/graphql-cli)
- [F# GraphQL](https://github.com/fsprojects/FSharp.Data.GraphQL)
- [Snowflaqe](https://github.com/Zaid-Ajaj/Snowflaqe)
- [Designing Graphql Mutations](https://blog.apollographql.com/designing-graphql-mutations-e09de826ed97)
- [System.IdentityModel.Tokens.Jwt](https://docs.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt?view=azure-dotnet)
- [MongoDB Pagination](https://www.codementor.io/@arpitbhayani/fast-and-efficient-pagination-in-mongodb-9095flbqr)
