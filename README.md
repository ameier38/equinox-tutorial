# Equinox Tutorial
[![Codefresh build status]( https://g.codefresh.io/api/badges/pipeline/ameier38/ameier38%2Fequinox-tutorial%2Fequinox-tutorial?type=cf-1)]( https://g.codefresh.io/public/accounts/ameier38/pipelines/ameier38/equinox-tutorial/equinox-tutorial)
___
Practical example for learning how to use the Jet.com Equinox library.

Features
- Working API using [Jet.com's Equinox library](https://github.com/jet/equinox)
- Bi-Temporal domain
- Fully Dockerized
- Integration tests and example CI using Codefresh
- Fully documented API at https://app.swaggerhub.com/apis-docs/ameier38/Lease/1.0.0
- Type safe DTOs using [OpenAPI Type Provider](https://github.com/fsprojects/OpenAPITypeProvider)
- Build automation using [FAKE](https://github.com/fsharp/FAKE)

The domain model is a lease, such as a car lease.

A more detailed explanation about the motivation for modeling a bi-temporal domain
can be found in this [blog post](https://andrewcmeier.com/bi-temporal-event-sourcing).

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
├── Lease.Service.fs        --> Functions for handling commands
├── Lease.Api.fs            --> Route handlers
└── Program.fs              --> Application entry point
```

## Dependencies
For running the API:
- [Docker](https://andrewcmeier.com/win-dev#docker)

For development:
- [dotnet CLI](https://github.com/dotnet/core-sdk)
> You will need version 2.1.6 for anonymous record support.
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
> You will need to install the dev dependencies first listed above.

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
  "startDate": "2017-07-21Z",
  "maturityDate": "2018-07-21Z",
  "monthlyPaymentAmount": 25
}'
```

## Resources
- [Equinox](https://github.com/jet/equinox)
- [Event Sourcing Basics](https://eventstore.org/docs/event-sourcing-basics/index.html)
- [12 Things You Should Know About Event Sourcing](https://blog.leifbattermann.de/2017/04/21/12-things-you-should-know-about-event-sourcing/)
