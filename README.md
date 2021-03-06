# Equinox Tutorial
Practical example for learning how to model a bi-temporal domain using
[Jet.com's Equinox Event Sourcing Platform](https://github.com/jet/equinox).

__Features__
- Bi-temporal, event sourced API in F# using 
[Jet.com's Equinox platform](https://github.com/jet/equinox) 
and [gRPC](https://grpc.io/) :milky_way:.
- Protobuf files compiled with 
[Uber's Prototool](https://github.com/uber/prototool) :wrench:.
- Fully typed GraphQL API using 
[FSharp.Data.GraphQL](https://github.com/fsprojects/FSharp.Data.GraphQL) :fireworks:.
- Centralized logging with [Seq](https://datalust.co/seq) :scroll:.
- Web application using [React](https://reactjs.org/) 
and [TypeScript](https://www.typescriptlang.org/) :computer:.
- Fully Dockerized for fast local development :whale:.
- Integration tests and example CI using [GitHub Actions](https://github.com/features/actions) :zap:.
- Build automation using [FAKE](https://github.com/fsharp/FAKE) :hammer:.

> The domain model is a lease, such as a car lease. 
A more detailed explanation about the motivation for modeling a bi-temporal domain 
can be found in the accompanying 
[blog post](https://andrewcmeier.com/bi-temporal-event-sourcing).

## TL;DR
```
docker-compose up -d --build web-app
```
Endpoints:
- http://localhost:2113 -> Event Store UI (database; user:admin password:changeit)
- http://localhost:8081 -> Seq UI (logging)
- http://localhost:3000 -> Web application

## Structure
```
equinox-tutorial
├── README.md           --> You are here
├── .github             --> CI/CD
├── docker-compose.yml  --> Dockerization
├── web-app             --> Web application
├── graphql-api         --> GraphQL API
├── lease-api           --> Lease gRPC API (business logic)
└── protos              --> Protobuf files
```

## Screenshots
__Application__
![app](./images/app.png)

__GraphQL Playground__
![playground](./images/playground.png)

__Seq Logs__
![logs](./images/logs.png)

__Event Store__
![eventstore](./images/eventstore.png)

## Time Travel
![time-travel](./images/time-travel.gif)

## Resources
- [Equinox](https://github.com/jet/equinox)
- [Event Sourcing Basics](https://eventstore.org/docs/event-sourcing-basics/index.html)
- [12 Things You Should Know About Event Sourcing](https://blog.leifbattermann.de/2017/04/21/12-things-you-should-know-about-event-sourcing/)
- [As Time Goes By Talk](https://youtu.be/xzekp1RuZbM)
- [GitHub Workflow Syntax](https://help.github.com/en/articles/workflow-syntax-for-github-actions)
