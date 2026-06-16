# Architecture

Clean Architecture with CQRS. Dependencies point inward only.

```
Api ──▶ Application ──▶ Domain
 │            ▲
 └──▶ Infrastructure ──┘
```

## Layers

| Project | Responsibility | Depends on |
|---|---|---|
| `Logistics.Domain` | Entities, value objects, domain rules, events. No external deps. | — |
| `Logistics.Application` | Use cases (CQRS commands/queries via MediatR), interfaces, validation. | Domain |
| `Logistics.Infrastructure` | Neo4j driver, repositories (Cypher), constraints bootstrap, external services. | Application |
| `Logistics.Api` | HTTP surface: controllers, contracts (DTOs), middleware, DI composition. | Application, Infrastructure |

## CQRS flow

1. Controller maps the request DTO → a `Command`/`Query` and sends it through MediatR (`ISender`).
2. `ValidationBehaviour` runs FluentValidation validators in the pipeline.
3. The handler executes the use case against an interface (e.g. `IRouteRepository`).
4. Infrastructure implements that interface with Cypher — the inner layers never see Neo4j types.

**Commands** mutate state (`Routes/Commands/CreateRoute`). **Queries** read (`Routes/Queries/GetShortestPath`, `Analytics/Queries`).

## Key conventions

- One feature = one folder holding its command/query + handler + validator (vertical slice inside the layer).
- All Cypher lives in `Infrastructure/Persistence/Neo4j/Cypher` — never in handlers or controllers.
- `Neo4jContext` owns a single thread-safe `IDriver` (singleton); sessions are short-lived per unit of work.
- Constraints/indexes are created idempotently on startup by `GraphConstraintsInitializer` (an `IHostedService`).
- Errors surface as RFC 7807 ProblemDetails via `ExceptionHandlingMiddleware`.
