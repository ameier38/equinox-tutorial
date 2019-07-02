# Equinox Tutorial
[![Codefresh build status]( https://g.codefresh.io/api/badges/pipeline/ameier38/ameier38%2Fequinox-tutorial%2Fequinox-tutorial?type=cf-1)]( https://g.codefresh.io/public/accounts/ameier38/pipelines/ameier38/equinox-tutorial/equinox-tutorial)
___
Practical example for learning how to model a bi-temporal domain.

Features
- Bi-temporal event sourcing using 
[Jet.com's Equinox library](https://github.com/jet/equinox).
- [gRPC](https://grpc.io/) backend API.
- Protobuf files leveraging [Uber's Prototool](https://github.com/uber/prototool).
- Fully typed GraphQL API using 
[FSharp.Data.GraphQL](https://github.com/fsprojects/FSharp.Data.GraphQL).
- Centralized logging with [Seq](https://datalust.co/seq).
- Web application using [React](https://reactjs.org/) 
and [TypeScript](https://www.typescriptlang.org/).
- Fully Dockerized for fast local development.
- Integration tests and example CI using [Codefresh](https://codefresh.io/).
- Build automation using [FAKE](https://github.com/fsharp/FAKE).

The domain model is a lease, such as a car lease.

A more detailed explanation about the motivation for modeling a bi-temporal domain
can be found in this [blog post](https://andrewcmeier.com/bi-temporal-event-sourcing).

## TL;DR
```
docker-compose up -d
```
Endpoints:
- http://localhost:2113 -> Event Store UI (database)
  > user:admin password:changeit
- http://localhost:5341 -> Seq UI (logging)
- http://localhost:8080 -> GraphQL Playground
- http://localhost:3000 -> Web application

## Structure
```
equinox-tutorial
├── README.md           --> You are here
├── codefresh.yml       --> CI/CD
├── docker-compose.yml  --> Dockerization
├── app                 --> Web application
├── graphql-api         --> GraphQL API
├── lease-api           --> Lease gRPC API (business logic)
└── proto               --> Protobuf files
```

## Resources
- [Equinox](https://github.com/jet/equinox)
- [Event Sourcing Basics](https://eventstore.org/docs/event-sourcing-basics/index.html)
- [12 Things You Should Know About Event Sourcing](https://blog.leifbattermann.de/2017/04/21/12-things-you-should-know-about-event-sourcing/)
- [Uber Protobuf Style Guide V2](https://github.com/uber/prototool/tree/dev/style)
- [GraphQL CLI](https://github.com/graphql-cli/graphql-cli)
- [Apollo GraphQL](https://www.apollographql.com/docs/react/)
