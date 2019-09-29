# Lease API
gRPC API for interacting with leases.

## Usage
Start EventStore.
```
docker-compose up -d eventstore
```

Start the Lease API.
```
fake build -t Serve
```
> The gRPC server listens on port 50051.

## Development

### Setup
Install the .NET SDK. and Docker Desktop
```
choco install -y dotnetcore-sdk
choco install -y docker-desktop
```
> Commands must be run as Administrator

### Updating protobuf files
Update the protobuf files and generate the new outputs. 
See [proto README](../proto/README.md) for instructions.

Then copy the generated files.
```
fake build -t CopyGenerated
```

### Testing
Start Event Store.
```
docker-compose up -d eventstore
```

Run the test suite.
```
fake build -t Test
```

## Resources
- [Jet.com's Equinox](https://github.com/jet/equinox)
- [Event Store](https://eventstore.org/)
- [Event Store Helm chart](https://github.com/EventStore/EventStore.Charts)
- [Bi-Temporal Event Sourcing](https://andrewcmeier.com/bi-temporal-event-sourcing)
- [gRPC status codes](https://github.com/grpc/grpc/blob/master/doc/statuscodes.md)
- [gRPC health checks](https://kubernetes.io/blog/2018/10/01/health-checking-grpc-servers-on-kubernetes/)
- [gRPC health probe](https://github.com/grpc-ecosystem/grpc-health-probe/)
- [gRPC TestServerCallContext](https://grpc.github.io/grpc/csharp/api/Grpc.Core.Testing.TestServerCallContext.html)
- [.NET gRPC health probe](https://github.com/grpc/grpc/blob/master/src/csharp/Grpc.HealthCheck/HealthServiceImpl.cs)
- [You're better off using exceptions](https://eiriktsarpalis.wordpress.com/2017/02/19/youre-better-off-using-exceptions/)
- [gRPC on dotnetcore](https://grpc.io/blog/grpc-on-dotnetcore/)
