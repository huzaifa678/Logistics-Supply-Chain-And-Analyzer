# logistics Helm chart

Deploys the **API** + **Angular frontend** (first-party images) and pulls **Neo4j, Redis,
RabbitMQ and Kafka** as upstream chart **dependencies** (no hand-written manifests for them).

## Layout
```
Chart.yaml      # dependencies: neo4j, redis, rabbitmq, kafka (each gated by <name>.enabled)
values.yaml     # api/frontend images, probes, ingress, app config, subchart values
templates/
  api-deployment.yaml      # Deployment (startup/readiness/liveness probes) + Service
  api-config.yaml          # ConfigMap (+ Secret) wiring app settings to subchart services
  frontend-deployment.yaml # Deployment (readiness/liveness) + Service
  ingress.yaml             # /api -> API, / -> frontend (same-origin, no CORS)
```

## Probes (backend)
The API exposes:
- `GET /health/live` — liveness (process up; no dependency checks).
- `GET /health/ready` — readiness (Neo4j reachable). A pod that can't reach Neo4j is pulled from
  the Service endpoints instead of serving failing requests.

Probe timings are tunable under `api.probes.*` in `values.yaml`.

## Install
```bash
# 1. Fetch the dependency charts into charts/
helm dependency update deploy/logistics-chart

# 2. Build & load your images (or push to a registry and set api.image / frontend.image)
docker build -t logistics-api:latest  -f deploy/Dockerfile .
docker build -t logistics-frontend:latest ./frontend

# 3. Install
helm install logistics deploy/logistics-chart \
  --set secrets.neo4jPassword='<strong-pass>' \
  --set neo4j.neo4j.password='<strong-pass>' \
  --set secrets.jwtSigningKey='<32+ byte key>'
```

Enable the Kafka event bus (needs a Schema Registry for Avro):
```bash
helm upgrade logistics deploy/logistics-chart \
  --set kafka.enabled=true \
  --set config.kafka.enabled=true \
  --set config.kafka.schemaRegistryUrl='http://<schema-registry>:8081'
```

## Notes / production hardening
- Values are **dev-grade**: Redis auth off, RabbitMQ uses `guest`, single-replica brokers, no TLS.
  For production: enable auth/TLS on the subcharts, use real Secrets (external-secrets / sealed-
  secrets), set persistence and replica counts, and pin chart + image versions.
- `secrets.neo4jPassword` must match `neo4j.neo4j.password` (the API and the Neo4j subchart share it).
- Bitnami moved its free images to `bitnamilegacy` (Aug 2025); set each subchart's `image.repository`
  accordingly, or point at your own mirror, if the default images don't pull.
- The Kafka subchart (Bitnami) has no Schema Registry. To use Avro events, run a Schema Registry
  (e.g. Confluent's, or Redpanda which bundles one) and set `config.kafka.schemaRegistryUrl`.
