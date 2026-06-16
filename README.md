# Logistics Analyzer

Supply chain logistics & analyzer built on **ASP.NET Core (.NET 10)** and **Neo4j**,
following Clean Architecture + CQRS.

## What it does

Models a logistics network as a graph — **warehouses** (nodes) connected by weighted
**routes** (edges), with **shipments** moving across it. Over that graph it exposes a secured,
rate-limited REST API to:

- Manage routes and run graph analytics — **weighted shortest path** between warehouses.
- Create and track **shipments** with an enforced status lifecycle, plus memory-safe
  streaming of large result sets.
- **Estimate** route duration & cost per transport mode, and **score** shipment delivery risk
  (distance, handoffs, mode, delays, warehouse congestion).
- Authenticate users via JWT access + rotating refresh tokens with role-based access.

## Solution layout

```
src/
  Logistics.Domain          # Entities, value objects, domain rules (no deps)
  Logistics.Application     # CQRS use cases (MediatR), interfaces, validation
  Logistics.Infrastructure  # Neo4j driver, repositories (Cypher), constraints
  Logistics.Api             # Controllers, DTOs, middleware, DI composition
tests/
  Logistics.Domain.UnitTests
  Logistics.Application.UnitTests
  Logistics.Infrastructure.IntegrationTests   # Testcontainers + real Neo4j (needs Docker)
  Logistics.Api.FunctionalTests               # in-memory WebApplicationFactory
```

Docs: [architecture](docs/architecture.md) · [graph model](docs/graph-model.md) · [auth model](docs/auth-model.md) · [engineering practices (SOLID, patterns, concurrency, DB)](docs/engineering-practices.md).

## Run locally

Start Neo4j (+ the API) with Docker:

```bash
docker compose -f deploy/docker-compose.yml up --build
```

Or run just the database and launch the API from the SDK:

```bash
docker compose -f deploy/docker-compose.yml up neo4j -d
dotnet run --project src/Logistics.Api
```

- API (dev): `https://localhost:<port>` — OpenAPI at `/openapi/v1.json`
- Neo4j browser: `http://localhost:7474`

Set the Neo4j password via `Neo4j__Password` (env) or `appsettings.json` → `Neo4j:Password`.

## Build & test

```bash
dotnet build LogisticsAnalyzer.slnx

# Unit tests (no Docker)
dotnet test tests/Logistics.Domain.UnitTests
dotnet test tests/Logistics.Application.UnitTests

# Integration tests (Docker must be running)
dotnet test tests/Logistics.Infrastructure.IntegrationTests
```

## Sample endpoints

```http
POST /api/routes
{ "originWarehouseId": "w1", "destinationWarehouseId": "w2",
  "distanceKm": 120.5, "cost": 300, "mode": 0 }

GET  /api/routes/shortest-path?origin=w1&destination=w2
```
