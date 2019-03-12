# Equinox Tutorial
Tutorial for Jet.com Equinox library.

The domain model is a lease, such as a car lease.

## Structure
```
Lease
├── paket.references        --> Dependencies
├── openapi.yaml            --> Available endpoints in OpenAPI config
├── Lease.Config.fs         --> Application configuration
├── Lease.SimpleTypes.fs    --> Definitions for simple types and measures
├── Lease.Domain.fs         --> Lease commands, events, and possible states
├── Lease.Dto.fs            --> Data transfer objects
├── Lease.Aggregate.fs      --> Main business logic
├── Lease.Store.fs          --> Set up for Event Store
├── Lease.Service.fs        --> Interfaces executing commands and running queries
├── Lease.Api.fs            --> Route handlers
└── Program.fs              --> Application entry point
```

## Dependencies
- [dotnet CLI](https://github.com/dotnet/core-sdk#installers-and-binaries)
> You will need version 2.1.6 for anonymous record support.
- [Docker](https://andrewcmeier.com/win-dev#docker)
- [FAKE](https://andrewcmeier.com/how-to-fake)

## Testing
Start Event Store.
```shell
docker-compose up -d eventstore
```

Build test image.
```shell
docker-compose build test
```

Run the test image.
```shell
docker-compose run --rm test
```

Alternatively you can run the test scripts yourself.
```shell
fake build -t test
```

## Usage
Start Event Store and the API.
```shell
docker-compose up -d
```

All the available endpoints are documented via SwaggerHub 
[here](https://app.swaggerhub.com/apis-docs/ameier38/Lease/1.0.0).

Create a lease.
```shell
curl -X POST \
  http://localhost:8080/lease \
  -H 'Content-Type: application/json' \
  -d '{
  "leaseId": "d290f1ee-6c54-4b01-90e6-d701748f0851",
  "startDate": "2017-07-21T17:32:28Z",
  "maturityDate": "2018-07-21T17:32:28Z",
  "monthlyPaymentAmount": 25
}'
```

## Resources
- [Equinox](https://github.com/jet/equinox)
