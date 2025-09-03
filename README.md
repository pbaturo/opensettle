# OpenSettle — Fintech Platform Blueprint (.NET 9 + Azure)

![CI](https://github.com/pbaturo/opensettle/actions/workflows/ci.yml/badge.svg)
![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)

**OpenSettle** is a reference platform for **payments & settlement** aimed at demonstrating:
- **Modern .NET 9** (Minimal APIs, EF Core, async messaging)
- **SRE foundations** (SLO/SLI, runbooks, observability with **OpenTelemetry**)
- **DevOps/IaC** (Docker, GitHub Actions, Bicep/Terraform, Azure Container Apps/AKS)
- **Security & Compliance mindset** (PII basics, auditability)
- **Event-first design** (Kafka contracts, idempotency, outbox)
- **Engineering hygiene** (ADRs, error models, versioning)

> Status: foundations in progress (Money lib, ADRs, Kafka infra). API/services land next.

---

## Architecture (overview)

```mermaid
flowchart LR
  Client -->|HTTPS| API[Payments API (.NET 9)]:::svc
  API -->|Events| K[Kafka (Redpanda)]:::dep
  API --> DB[(PostgreSQL)]:::dep
  API --> C[(Redis)]:::dep
  Worker[Payments Worker]:::svc -->|Consumes| K
  Worker -->|Park poison| KDLQ[(payments.created.dlq)]:::dep

  subgraph Observability
    OTel[OpenTelemetry]:::obs --> Sink[(Jaeger / OTLP / Azure Monitor)]:::dep
  end

  API -- traces/logs/metrics --> OTel
  Worker -- traces/logs/metrics --> OTel

  classDef svc fill:#eef,stroke:#88f,stroke-width:1px,color:#111;
  classDef dep fill:#efe,stroke:#6b6,stroke-width:1px,color:#111;
  classDef obs fill:#ffe,stroke:#fc3,stroke-width:1px,color:#111;
```

> Local dev uses **Redpanda** (Kafka-compatible). Azure targets (ACA/AKS, Key Vault, Front Door) will be added later.

## Quickstart (local)

### infra (kafka, db, redis, jaeger)

**Up all infra (broker, db, cache, tracing)**
docker compose -f infra/compose/docker-compose.yml up -d

**Down (keep volumes)**
docker compose -f infra/compose/docker-compose.yml down

### restore + run API
dotnet restore
dotnet run --project src/services/payments/Payments.Api
