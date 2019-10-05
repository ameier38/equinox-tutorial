# GraphQL API
Main client facing API which stitches together the different gRPC APIs.

## Development
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

## Usage
Start services.
```
docker-compose up -d eventstore
docker-compose up -d lease-api
docker-compose up -d graphql-playground
```

Start the GraphQL API.
```
fake build -t Serve
```

## Resources
- [GraphQL](https://graphql.org/)
- [GraphQL CLI](https://github.com/graphql-cli/graphql-cli)
- [Designing Graphql Mutations](https://blog.apollographql.com/designing-graphql-mutations-e09de826ed97)
