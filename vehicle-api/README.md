# Vehicle Service
gRPC API to manage vehicles.

## Setup
1. Install [.Net Core SDK](https://andrewmeier.dev/win-dev#dotnet)
2. Install [FAKE](https://andrewmeier.dev/win-dev#fake)
3. Install [Paket](https://andrewmeier.dev/win-dev#paket)

## Testing
Run the unit tests.
```
fake build -t TestUnits
```

Bring up server for integration testing.
```
docker-compose up -d --build vehicle-api
```

Run the integration tests.
```
fake build -t TestIntegrations
```

## Development
- Don't use reflection. In some cases it can slow down the app and
it could cause issues when building with `PublishTrimmed` option.