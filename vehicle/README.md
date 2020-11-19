# Vehicle Service
The vehicle service consists of two parts.
1. **Processor**: A gRPC server to _process_ vehicle commands.
2. **Reactor**: A console application to _react_ to vehicle events and update read models in MongoDB.
3. **Reader**: A gRPC server to _read_ vehicle read models.

## Setup
1. Install [.Net Core SDK](https://andrewmeier.dev/win-dev#dotnet).
2. Install tools.
    ```shell
    dotnet tool restore
    ```

## Testing
Run the unit tests.
```
dotnet fake build -t TestUnits
```

Bring up server and reactor for integration testing.
```
docker-compose up -d --build vehicle-processor vehicle-reactor vehicle-reader
```

Run the integration tests.
```
dotnet fake build -t TestIntegrations
```

## Resources
- [Equinox](https://github.com/jet/equinox)
- [EventStore Samples](https://github.com/EventStore/EventStore.Samples.Dotnet)
- [API Pagination](https://cloud.google.com/apis/design/design_patterns#list_pagination)
