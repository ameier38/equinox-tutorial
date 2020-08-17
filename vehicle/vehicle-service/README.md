# Vehicle Service
The vehicle service consists of two parts.
1. A gRPC server to handle vehicle commands.
2. A reactor process to handle updating read models in MongoDB.

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
docker-compose up -d --build vehicle-api vehicle-reactor
```

Run the integration tests.
```
fake build -t TestIntegrations
```

## Resources
- [EventStore Samples](https://github.com/EventStore/EventStore.Samples.Dotnet)
