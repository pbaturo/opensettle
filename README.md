# OpenSettle — Fintech Platform Blueprint (.NET 8 + Azure)

![CI](https://github.com/<your-username>/opensettle/actions/workflows/ci.yml/badge.svg)
![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)

**OpenSettle** is a reference platform for **payments & settlement** aimed at demonstrating:
- **Modern .NET 8/9** (Minimal APIs, EF Core, async messaging)
- **SRE foundations** (SLO/SLI, runbooks, observability with **OpenTelemetry**)
- **DevOps/IaC** (Docker, GitHub Actions, Bicep/Terraform, Azure Container Apps/AKS)
- **Security & Compliance mindset** (PII basics, auditability)


---

## Architecture (overview)

```mermaid
flowchart LR
  Client -->|HTTPS| API[Payments API (.NET 8)]
  API -->|Commands| Bus[Azure Service Bus]
  API --> DB[(PostgreSQL)]
  API --> Cache[(Redis)]
  Worker[Billing Worker] -->|Subscriptions| Bus
  subgraph Observability
    OTel[OpenTelemetry] --> Jaeger[(Jaeger/OTLP)]
  end
  API -- traces/logs/metrics --> OTel
  Worker -- traces/logs/metrics --> OTel
```

## Quickstart (local)

### infra (db, redis, jaeger)
docker compose -f infra/docker-compose.yml up -d

### restore + run API
dotnet restore
dotnet run --project src/services/payments/Payments.Api
