# GraphQL API
Main client facing API which stitches together the different gRPC APIs.

## Development

### Setup
Install the .NET SDK.
```shell
choco install dotnetcore-sdk
```
> Must be run as administrator

Install FAKE.
```shell
dotnet tool install fake-cli -g
```

Install Paket.
```shell
dotnet tool install paket -g
```

Add tool path to `PATH`.

_Linux/macOS_
```shell
export PATH = "$PATH:$HOME/.dotnet/tools"
```
_Windows Powershell_
```powershell
$env:PATH += ";C:/Users/<user>/.dotnet/tools"
```

Install [GraphQL Playground](https://github.com/prisma-labs/graphql-playground)
> Used for interactively running queries.

## Usage
Start server.
```
docker-compose up -d graphql-api
```

Run tests.
```
fake build -t Test
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
- [GraphQL](https://graphql.org/)
- [GraphQL CLI](https://github.com/graphql-cli/graphql-cli)
- [Designing Graphql Mutations](https://blog.apollographql.com/designing-graphql-mutations-e09de826ed97)
