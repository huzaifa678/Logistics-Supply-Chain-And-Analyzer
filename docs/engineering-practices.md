# Engineering Practices

How this codebase applies SOLID, design patterns, concurrency, and data-access best practices.

## 1. SOLID

| Principle | How it shows up |
|---|---|
| **S**ingle Responsibility | Each handler does one use case; Cypher lives in `*Cypher` classes; record↔entity mapping lives in `Mappings/*Mapper`; HTTP shaping lives in controllers/contracts. A repository never parses records and a handler never sees Cypher. |
| **O**pen/Closed | New domain-event reactions = add an `IDomainEventHandler` (auto-discovered in DI); the `DomainEventProcessor` never changes. New MediatR pipeline steps = add an `IPipelineBehavior`. New features = new folder, no edits to existing ones. |
| **L**iskov | Repositories are substitutable behind their interfaces — unit tests inject hand-rolled fakes; production injects Neo4j implementations. Nothing depends on a concrete repo. |
| **I**nterface Segregation | Narrow, role-specific interfaces: `IRouteRepository`, `IGraphAnalyticsRepository`, `IPasswordHasher`, `IJwtTokenGenerator`, `ISecureTokenGenerator` — no fat "I-do-everything" service. |
| **D**ependency Inversion | Inner layers define interfaces; `Infrastructure` implements them. The dependency graph points inward (`Api → Application → Domain`, `Infrastructure → Application`). Domain references nothing external. |

## 2. Design patterns (used where they pay off, not everywhere)

| Pattern | Location | Why |
|---|---|---|
| **Clean / Onion architecture** | project layering | Keeps Neo4j swappable and the domain pure. |
| **CQRS + Mediator** | `Application/**/Commands`,`Queries` via MediatR | Separates writes from reads; thin controllers. |
| **Repository** | `*Repository` + interfaces | Abstracts persistence; enables fakes in tests. |
| **DAO + Mapper** | `Persistence/Neo4j/Mappings` | Isolates the driver's record shape from the domain; centralizes conversion. |
| **DTO** | `Api/Contracts`, query `*Dto` records | Decouples wire/read models from entities. |
| **Factory Method** | `Entity.Create(...)` static factories | Enforces invariants at construction; `Rehydrate` skips them when loading. |
| **State** | `Shipment` status transitions | The entity rejects illegal transitions; status logic is in one place. |
| **Domain Service** | `ShipmentRoutingService`, `ShipmentRiskService` | Cross-aggregate logic that belongs to no single entity. |
| **Adapter (Ports & Adapters)** | `GraphAnalyticsRouteGraphAdapter`, `WarehouseCongestionAdapter` | Adapt the DB-facing repository to the domain's `IRouteGraph` / `IWarehouseCongestionProvider` ports. |
| **Strategy** | `ITransportModeProfile`, `IRiskFactor` | Per-mode speed/cost and composable risk rules; new rule = new class, no service edits. |
| **Pipeline / Decorator** | `ValidationBehaviour` | Cross-cutting validation without touching handlers. |
| **Result** | `Result<T>` | Models expected failure without exceptions. |
| **Producer/Consumer (Queue)** | `IDomainEventQueue` + `DomainEventProcessor` | Moves side effects off the request thread. |
| **Options** | `Neo4jSettings`, `AuthSettings` | Typed, validated configuration. |

Patterns deliberately **avoided**: a heavy OGM, a generic `IRepository<T>` base (leaky for graph traversals), and an `IUnitOfWork` abstraction over Neo4j (the driver's transaction function already *is* the unit of work).

## 3. Service layer (and what it is *not*)

> In CQRS + MediatR, the **handlers are the application-service layer** — one class per use
> case. We deliberately do **not** wrap them in a parallel `FooService`; that's indirection
> with no behavior. A service layer is added only where it carries real logic:

- **Domain services** — business logic spanning multiple aggregates, owned by no single entity.
  `ShipmentRoutingService` combines a graph path with mode-specific speed/cost rules to estimate
  duration and cost. It's **pure**: it depends on the `IRouteGraph` port and the transport-mode
  strategies, never on MediatR, the database, or HTTP — so it unit-tests with simple fakes.

- **The DB↔service seam is an Adapter (Ports & Adapters / Hexagonal).** The domain declares the
  port it needs (`IRouteGraph`). `GraphAnalyticsRouteGraphAdapter` (Application layer) implements
  that port by delegating to `IGraphAnalyticsRepository` and translating the persistence DTO into
  the domain type. Net effect:

  ```
  EstimateRouteQueryHandler          (use-case orchestration)
        │ depends on
        ▼
  IShipmentRoutingService  ──▶  IRouteGraph (port, Domain)
   (domain service)                  ▲
                                     │ implements (Adapter)
                          GraphAnalyticsRouteGraphAdapter (Application)
                                     │ delegates to
                                     ▼
                          IGraphAnalyticsRepository ──▶ Neo4j (Infrastructure)
  ```

  The service never sees a repository, Cypher, or the driver; the repository never sees a domain
  service. Swap Neo4j for anything else by writing one new adapter — nothing in the domain changes.

- **The same treatment, repeated for risk scoring.** `ShipmentRiskService` composes a set of
  `IRiskFactor` strategies (distance, handoffs, mode, delay status, endpoint congestion) into a
  0–100 score + band. It pulls route data through `IRouteGraph` and congestion through a second
  port, `IWarehouseCongestionProvider`, satisfied by `WarehouseCongestionAdapter` over the same
  analytics repository. Adding a risk rule is a new `IRiskFactor` class + one DI line — the service
  is closed for modification. Endpoint: `GET /api/shipments/{id}/risk`.

## 4. Concurrency & threading (ASP.NET Core reality)

> **You do not manage server/listener threads in ASP.NET Core.** Kestrel runs an async
> event loop over the thread pool and assigns requests for you. Manually spawning threads
> per request or per layer *reduces* throughput (pool starvation, context-switching) and is
> an anti-pattern. The correct tools are async/await, the thread pool, and background workers.

What this codebase does:

- **Async all the way.** Every I/O path is `async`/`await` with a `CancellationToken` flowing from the controller down to the driver. No `.Result`, `.Wait()`, or `async void`. No sync-over-async.
- **Background worker (the legit "worker thread").** `DomainEventProcessor : BackgroundService` is a single long-running consumer that drains a channel and dispatches events — see `Infrastructure/Messaging`. Slow side effects (notifications, ETA recompute) never block the HTTP response.
- **Producer/consumer via `System.Threading.Channels`.** `ChannelDomainEventQueue` is a **bounded** channel (`capacity 1024`, `FullMode.Wait`) giving **back-pressure** instead of unbounded memory growth. Lock-free, many writers / one reader.
- **Scope-per-event.** The worker opens a fresh DI scope per event so scoped services (repositories) are isolated and thread-safe.
- **Graceful shutdown.** The worker honors the host's stopping token and exits the loop cleanly.
- **Fan-out when needed.** For CPU-bound or independent I/O fan-out, use `await Task.WhenAll(...)` or `Parallel.ForEachAsync` with a bounded `MaxDegreeOfParallelism` — not raw `new Thread(...)`.

### Where you *would* add more workers
- Scale the processor to N concurrent consumers by reading the channel from multiple loops (set `SingleReader = false`).
- For cross-instance durability, the in-memory channel can still be fronted by a transactional outbox; the broker integration below already gives durable cross-service delivery.

## 5. Message brokers (Kafka + RabbitMQ)

The in-process channel (§4) dispatches events **within** this service. For durable, cross-service
delivery there are two brokers, split by purpose and each behind a swappable port:

| Concern | Broker | Port | Why |
|---|---|---|---|
| Integration **event** backbone | **Kafka** | `IIntegrationEventPublisher` | Durable, partitioned, replayable log — fits supply-chain event streams. |
| **Notification** delivery | **RabbitMQ** | `INotificationPublisher` | Work queue with acks/requeue — fits one-off delivery to email/SMS/push. |

End-to-end pipeline:

```
Shipment.MarkDelayed()  ──raises──▶  ShipmentDelayedEvent (domain)
   in-process worker ▶ PublishShipmentDelayedHandler ▶ IIntegrationEventPublisher (Kafka topic)
        KafkaIntegrationEventConsumer ▶ INotificationPublisher (RabbitMQ queue)
             RabbitMqNotificationConsumer ▶ deliver (email/SMS/push)
```

- **Swappable / DIP.** Handlers and consumers depend on the ports, not Kafka/RabbitMQ types. To
  move the event bus to RabbitMQ (or notifications to Kafka), add another implementation of the
  same interface and flip config — nothing else changes.
- **Safe default off.** `Messaging:Kafka:Enabled` / `Messaging:RabbitMq:Enabled` default to
  `false`; disabled buses bind `NoOp*Publisher` so local dev and tests need no broker (same
  pattern as the rate limiter).
- **Delivery semantics.** Kafka consumer commits offsets only after the notification is published;
  the RabbitMQ consumer acks only after delivery and nacks+requeues on failure — at-least-once
  end to end. Handlers should therefore be idempotent.
- **Connections.** One Kafka producer and one RabbitMQ connection per app lifetime (singletons);
  RabbitMQ channels are created per publish/consumer.

## 6. Neo4j read/write best practices

- **One driver, app-lifetime, singleton.** `Neo4jContext` wraps a single thread-safe `IDriver`. Sessions are **short-lived**, created per unit of work, and disposed with `await using`.
- **Connection pooling (tuned).** The driver multiplexes all sessions over one pool, configured in `Neo4jContext` from `Neo4jSettings`: `MaxConnectionPoolSize` (size to concurrency), `ConnectionAcquisitionTimeout` (fail fast when saturated), `ConnectionTimeout`, and `MaxConnectionLifetime` (recycle for rolling restarts / LB changes). Never open a driver per request — that defeats pooling.
- **Streaming reads (no full-buffer).** Large result sets are read with `IAsyncEnumerable` + the driver's `FetchAsync` loop (`ShipmentRepository.StreamByStatusAsync`), which pulls **one record at a time** and yields it mapped — the whole result is never materialized in memory. The query handler and controller keep it lazy end to end (the action returns `IAsyncEnumerable<T>`, so ASP.NET Core streams the JSON array). Use `ToListAsync` only for known-small results.
- **Route reads vs writes.** Reads use `AccessMode.Read` (can be served by replicas in a cluster); writes use `AccessMode.Write`. Use `ExecuteReadAsync` / `ExecuteWriteAsync`.
- **Transaction functions = automatic retry.** `Execute{Read,Write}Async` retries transient errors (leader switches, deadlocks) with backoff. **Consume the cursor inside the delegate** (`ToListAsync` / `SingleAsync` / `ConsumeAsync`) — never return a live `IResultCursor`, it's closed when the transaction commits.
- **Always parameterize Cypher.** Every query uses `$param` placeholders — never string interpolation. Prevents injection and lets the server cache query plans.
- **Versioned migrations.** `GraphMigrationRunner` applies ordered, idempotent `IGraphMigration`s on startup and records each as a `(:__Migration)` node so it runs once per database — schema (constraints/indexes) and data backfills alike. Traversal performance depends on the indexes from `0001_initial_schema`.
- **Atomic graph writes.** Linking writes (e.g. shipment → origin/destination) happen in a single Cypher statement so the graph is never left half-connected.
- **Idempotency / upserts.** Prefer `MERGE` over `CREATE` where re-runs are possible; keep `CREATE` where uniqueness constraints already guard duplicates.

## 7. Distributed rate limiting (Redis token bucket)

Per-client limiting that's correct across **multiple API instances** — an in-memory limiter
would let N replicas each allow the full quota.

- **Algorithm: token bucket.** A bucket of `Capacity` tokens refills at `RefillTokensPerSecond`. Each request spends one token; an empty bucket → `429`. This allows bursts up to capacity while bounding the sustained rate — better for APIs than a fixed window (no boundary spikes).
- **Atomicity via Lua.** The read→refill→spend→write sequence runs as a single Redis Lua script (`RedisTokenBucketRateLimiter`), so concurrent requests against the same bucket can't race or double-spend. Idle buckets self-expire via `EXPIRE`.
- **Partitioning.** `RateLimitingMiddleware` keys the bucket by authenticated user id when present, else client IP — so one client can't starve others. It runs after authentication, before controllers.
- **Responses.** Denied requests get `429` + `Retry-After`; allowed requests get an `X-RateLimit-Remaining` header.
- **Pluggable + safe default.** Behind `IRateLimiter` (Dependency Inversion). `RateLimiting:Enabled=false` swaps in a `NoOpRateLimiter` so local dev / tests need no Redis. The Redis connection uses a single app-lifetime `IConnectionMultiplexer` (its own pool).

## 8. Driver choice (the JDBC / JPA question)

| Java world | .NET equivalent | Used for |
|---|---|---|
| Neo4j **driver** (Bolt) / JDBC | **`Neo4j.Driver`** (official) | Analytics (shortest path), streaming reads, constraints. |
| **Spring Data Neo4j / JPA (OGM)** | **`Neo4jClient`** (typed fluent Cypher + POCO mapping) | CRUD repositories (Identity; extendable to Route/Shipment). |

**There is no EF-Core-style ORM for Neo4j** that hides Cypher entirely. `Neo4jClient` is the
closest OGM: a typed fluent query builder with automatic node↔POCO mapping. We run a deliberate
**hybrid**:

- **ORM (`Neo4jClient`)** for plain CRUD — `UserRepository` / `RefreshTokenRepository` build
  queries with the fluent API (`Cypher.Match(...).Where(...).Return(u => u.As<UserNode>())`) and
  map nodes automatically. No raw Cypher strings, no `record["x"].As<>()`. PascalCase node models
  map to camelCase Neo4j properties via a `CamelCasePropertyNamesContractResolver`.
- **Official driver** for the two things the ORM does worse:
  - **Streaming reads** — the ORM buffers full results; `ShipmentRepository.StreamByStatusAsync`
    needs the driver's `FetchAsync` cursor to avoid materializing large sets.
  - **Graph analytics** — shortest path / congestion are hand-written Cypher; an OGM would emit
    traversals you can't tune.

**One connection pool.** `Neo4jGraphClientProvider` constructs `BoltGraphClient` over the *same*
`IDriver` that `Neo4jContext` owns (`new BoltGraphClient(driver, …)`), so the ORM and driver paths
do not open separate pools. The client is connected once at startup by `GraphMigrationRunner`.

> Trade-off, stated plainly: the ORM removes mapping boilerplate for CRUD but can't stream and
> hides traversal Cypher — hence CRUD-only. Route/Shipment CRUD can move to the ORM the same way;
> analytics and streaming should stay on the driver.

## 9. Async checklist (for new code)

- [ ] Accept and pass a `CancellationToken` end to end.
- [ ] `await` every Task; never block (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`).
- [ ] Return `Task`/`ValueTask`; no `async void` except event handlers.
- [ ] Consume Neo4j cursors inside the transaction delegate.
- [ ] Don't capture scoped services in singletons — use `IServiceScopeFactory`.
- [ ] Offload slow side effects to the event queue, not the request thread.
