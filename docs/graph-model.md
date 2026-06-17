# Graph Model

## Nodes

| Label | Key properties |
|---|---|
| `Warehouse` | `id` (unique), `name`, `latitude`, `longitude`, `capacityUnits` |
| `Shipment` | `id` (unique), `trackingNumber` (unique), `weightKg`, `mode`, `status`, `createdAt`, `estimatedArrival?`, `deliveredAt?` |
| `User` | `id` (unique), `email` (unique), `passwordHash`, `displayName`, `role`, `createdAt` |
| `RefreshToken` | `id` (unique), `tokenHash` (indexed), `expiresAt`, `createdAt`, `revokedAt?`, `replacedByTokenId?` |
| `Supplier` | `id`, `name` (planned) |
| `Product` | `id`, `sku` (planned) |

## Relationships

| Pattern | Meaning | Properties |
|---|---|---|
| `(:Warehouse)-[:CONNECTS_TO]->(:Warehouse)` | A directed, weighted route | `id`, `distanceKm`, `cost`, `mode` |
| `(:Shipment)-[:ORIGINATES_AT]->(:Warehouse)` | Where a shipment starts | — |
| `(:Shipment)-[:DESTINED_FOR]->(:Warehouse)` | Where a shipment is headed | — |
| `(:User)-[:HAS_TOKEN]->(:RefreshToken)` | Refresh tokens owned by a user | — |
| `(:Shipment)-[:CARRIES]->(:Product)` | Manifest line (planned) | `quantity` |
| `(:Supplier)-[:SUPPLIES]->(:Product)` | Sourcing (planned) | — |

## Shipment lifecycle (application logic)

`Shipment` is an aggregate whose status transitions are enforced in the domain entity
(`Logistics.Domain/Entities/Shipment.cs`), not in Cypher:

```
Created ──Dispatch──▶ InTransit ──Deliver──▶ Delivered
   │                     │  ▲
 Cancel               Delay  └──Deliver── Delayed
   ▼                     ▼
Cancelled            Delayed
```

- `Dispatch` is only legal from `Created`; sets `EstimatedArrival`.
- `MarkDelayed` is only legal from `InTransit`; raises a `ShipmentDelayedEvent`.
- `Deliver` is legal from `InTransit` or `Delayed`; stamps `DeliveredAt`.
- `Cancel` is legal from any non-delivered state.

Illegal transitions throw and surface as `422 Unprocessable Entity` via the exception
middleware. Writes go through `IShipmentRepository`; the create query links the shipment to
both warehouses in a single Cypher statement so the graph is never left half-connected.

### Example create

```cypher
MATCH (o:Warehouse {id:$originId}), (d:Warehouse {id:$destinationId})
CREATE (s:Shipment { id:$id, trackingNumber:$trackingNumber, weightKg:$weightKg,
                     mode:$mode, status:$status, createdAt:$createdAt })
CREATE (s)-[:ORIGINATES_AT]->(o)
CREATE (s)-[:DESTINED_FOR]->(d)
```

### Useful analytics queries

```cypher
// Shipments currently delayed, with their route
MATCH (s:Shipment {status:'Delayed'})-[:ORIGINATES_AT]->(o)
MATCH (s)-[:DESTINED_FOR]->(d)
RETURN s.trackingNumber, o.name, d.name;

// Busiest warehouses by inbound + outbound shipment volume
MATCH (s:Shipment)-[r:ORIGINATES_AT|DESTINED_FOR]->(w:Warehouse)
RETURN w.name, count(s) AS volume ORDER BY volume DESC LIMIT 10;
```

## Migrations (schema + data)

Schema and data are versioned through ordered, idempotent **graph migrations** applied on
startup by `GraphMigrationRunner` (Neo4j has no built-in migration engine). Each applied
migration is recorded as a `(:__Migration {id, appliedAt})` node, so it runs once per database.

| Id | Type | What it does |
|---|---|---|
| `0001_initial_schema` | schema | Uniqueness constraints + indexes (Warehouse, Shipment, User, RefreshToken) |
| `0002_default_warehouse_capacity` | data | Backfills `capacityUnits = 0` on warehouses missing it |

To add one: implement `IGraphMigration` with the next `NNNN_*` id, register it in
`AddInfrastructure`, and keep it **idempotent** (`IF NOT EXISTS`, `MERGE`, `WHERE ... IS NULL`) —
the body and its `__Migration` record commit in separate transactions, so a crash between them
must be safe to re-run. Keep each migration schema-only or data-only (Neo4j forbids mixing both
in one transaction).

Example constraint (from `0001`):

```cypher
CREATE CONSTRAINT warehouse_id IF NOT EXISTS FOR (w:Warehouse) REQUIRE w.id IS UNIQUE;
CREATE INDEX warehouse_name IF NOT EXISTS FOR (w:Warehouse) ON (w.name);
```

## Analytics

Shortest path uses Cypher `shortestPath()` for modest graphs. For large graphs or
centrality / bottleneck / community detection, switch to the **Graph Data Science (GDS)**
library (enabled via the `graph-data-science` plugin in `deploy/docker-compose.yml`):

```cypher
CALL gds.shortestPath.dijkstra.stream(...) YIELD path
```

Wrap GDS calls in dedicated handlers under `Application/Analytics/Queries`.
