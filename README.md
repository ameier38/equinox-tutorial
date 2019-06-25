# Equinox Tutorial
[![Codefresh build status]( https://g.codefresh.io/api/badges/pipeline/ameier38/ameier38%2Fequinox-tutorial%2Fequinox-tutorial?type=cf-1)]( https://g.codefresh.io/public/accounts/ameier38/pipelines/ameier38/equinox-tutorial/equinox-tutorial)
___
Practical example for learning how to use the Jet.com Equinox library.

Features
- Working API using [Jet.com's Equinox library](https://github.com/jet/equinox)
- Bi-Temporal domain
- Fully Dockerized
- Integration tests and example CI using Codefresh
- Build automation using [FAKE](https://github.com/fsharp/FAKE)

The domain model is a lease, such as a car lease.

A more detailed explanation about the motivation for modeling a bi-temporal domain
can be found in this [blog post](https://andrewcmeier.com/bi-temporal-event-sourcing).

## Structure
```
equinox-tutorial
├── README.md           --> You are here
├── codefresh.yml       --> CI/CD
├── docker-compose.yml  --> Dockerization
├── graphql-api         --> GraphQL API
├── lease-api           --> Lease API (business logic)
└── proto               --> Protobuf files
```

## Resources
- [Equinox](https://github.com/jet/equinox)
- [Event Sourcing Basics](https://eventstore.org/docs/event-sourcing-basics/index.html)
- [12 Things You Should Know About Event Sourcing](https://blog.leifbattermann.de/2017/04/21/12-things-you-should-know-about-event-sourcing/)
