# Lease API
gRPC API for interacting with leases.

## Setup
Install the .NET SDK. and Docker Desktop
```
choco install -y dotnetcore-sdk
choco install -y docker-desktop
```
> Commands must be run as Administrator

Install FAKE.
```
dotnet tool install fake-cli -g
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
Start server.
```
docker-compose up -d lease-api
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

## Conventions
- Dates with time components are suffixed with `At`
- Dates without time components are suffix with `Date`

## Resources
- [Jet.com's Equinox](https://github.com/jet/equinox)
- [Event Store](https://eventstore.org/)
- [Event Store Helm chart](https://github.com/EventStore/EventStore.Charts)
- [Bi-Temporal Event Sourcing](https://andrewcmeier.com/bi-temporal-event-sourcing)
- [Retroactive and Future Events](https://www.infoq.com/news/2018/02/retroactive-future-event-sourced/)
- [gRPC status codes](https://github.com/grpc/grpc/blob/master/doc/statuscodes.md)
- [gRPC health checks](https://kubernetes.io/blog/2018/10/01/health-checking-grpc-servers-on-kubernetes/)
- [gRPC health probe](https://github.com/grpc-ecosystem/grpc-health-probe/)
- [gRPC TestServerCallContext](https://grpc.github.io/grpc/csharp/api/Grpc.Core.Testing.TestServerCallContext.html)
- [.NET gRPC health probe](https://github.com/grpc/grpc/blob/master/src/csharp/Grpc.HealthCheck/HealthServiceImpl.cs)
- [You're better off using exceptions](https://eiriktsarpalis.wordpress.com/2017/02/19/youre-better-off-using-exceptions/)
- [gRPC on dotnetcore](https://grpc.io/blog/grpc-on-dotnetcore/)
- [F# Expecto](https://github.com/haf/expecto)
- [Example lease contract](https://www.sec.gov/Archives/edgar/data/1365354/000119312506140498/dex1001.htm)
