# Vehicle Service
The vehicle service consists of two parts.
1. **Processor**: A gRPC server to process vehicle commands.
2. **Reactor**: A console application to react to vehicle events and update read models in MongoDB.

## Setup
1. Install [.Net Core SDK](https://andrewmeier.dev/win-dev#dotnet)
2. Install [FAKE](https://andrewmeier.dev/win-dev#fake)
3. Install [Paket](https://andrewmeier.dev/win-dev#paket)

## Testing
Run the unit tests.
```
fake build -t TestUnits
```

Bring up server and reactor for integration testing.
```
docker-compose up -d --build vehicle-processor vehicle-reactor
```

Run the integration tests.
```
fake build -t TestIntegrations
```

## Resources
- [Equinox](https://github.com/jet/equinox)
- [EventStore Samples](https://github.com/EventStore/EventStore.Samples.Dotnet)
