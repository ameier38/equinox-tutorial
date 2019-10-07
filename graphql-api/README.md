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

### Updating Protos
If you have updated the protocol buffers in the [protos sub-repo](../protos),
first make sure you have generated the compiled code then run the following
to copy the generated code and rebuild the project.
```
fake build -t UpdateProtos
```

## Usage
Start services.
```
docker-compose up -d lease-api
```

Start the GraphQL API.
```
fake build -t Serve
```

## Resources
- [GraphQL](https://graphql.org/)
- [GraphQL CLI](https://github.com/graphql-cli/graphql-cli)
- [Designing Graphql Mutations](https://blog.apollographql.com/designing-graphql-mutations-e09de826ed97)
