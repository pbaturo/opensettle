# Folder structure

opensettle/
├─ OpenSettle.sln
├─ OpenSettle.slnf                 # solution filter (np. tylko payments)
├─ global.json                     # pin wersji .NET SDK
├─ Directory.Packages.props        # central package mgmt (NuGet)
├─ NuGet.config                    # prywatne feedy (opcjonalnie)
├─ .editorconfig                   # styl kodu
├─ .gitattributes
├─ .gitignore
├─ .github/
│  └─ workflows/                   # ci.yml, cd.yml, codeql.yml
├─ .config/
│  └─ dotnet-tools.json            # dotnet-ef, dotnet-format, etc.
│
├─ src/
│  ├─ apps/
│  │  ├─ Admin.Web/                # (opcjonalnie) panel admin (React/Blazor) lub CLI
│  │  └─ Maintenance.Cli/          # narzędzia operacyjne
│  │
│  ├─ services/
│  │  ├─ Payments/
│  │  │  ├─ Payments.Api/          # Minimal APIs, auth, health, OTel
│  │  │  ├─ Payments.Application/  # use-cases/handlers
│  │  │  ├─ Payments.Domain/       # Entities/VO (np. Money), domain events
│  │  │  └─ Payments.Infrastructure/# EF Core, ASB, repo, migracje
│  │  ├─ Billing/
│  │  │  ├─ Billing.Worker/        # consumer/worker (MassTransit)
│  │  │  ├─ Billing.Application/
│  │  │  ├─ Billing.Domain/
│  │  │  └─ Billing.Infrastructure/
│  │  └─ Gateway/
│  │     └─ Gateway.Api/           # API gateway / BFF (opcjonalne)
│  │
│  ├─ building-blocks/             # tylko biblioteki współdzielone (brak zależności cyklicznych)
│  │  ├─ OpenSettle.Abstractions/  # Result, errors, paging, guards
│  │  ├─ OpenSettle.Messaging/     # MassTransit helpers, conventions
│  │  ├─ OpenSettle.Observability/ # OTel setup, Serilog, logging pipeline
│  │  ├─ OpenSettle.Security/      # auth helpers, policies, claims
│  │  └─ OpenSettle.Persistence/   # common EFCore infra (migrations base, interceptors)
│  │
│  └─ shared/                      # kontrakty .NET i utilsy „bez logiki biznesowej”
│     ├─ Contracts/                # np. eventy/DTO (jeśli nie trzymasz w /contracts jako schema)
│     └─ Utilities/
│
├─ contracts/                      # ŹRÓDŁO PRAWDY DLA KONTRAKTÓW
│  ├─ http/                        # OpenAPI/Swagger (yaml)
│  ├─ messaging/                   # event schemas (avro/json/proto) + wersjonowanie
│  └─ examples/                    # payloady przykładów
│
├─ tests/
│  ├─ Payments.UnitTests/
│  ├─ Payments.IntegrationTests/
│  ├─ Billing.UnitTests/
│  ├─ Contracts.Tests/             # Pact / schematy / backward-compat
│  └─ LoadTests/                   # k6/gatling (skrypty)
│
├─ infra/
│  ├─ docker/                      # obrazy dev, Dockerfile bazowe
│  ├─ compose/                     # docker-compose.*.yml (dev, local)
│  └─ k8s/                         # jeśli AKS; base + overlays (dev/stage/prod)
│
├─ deploy/
│  ├─ iac/
│  │  ├─ azure/
│  │  │  ├─ bicep/                 # landing zone, ACA/AKS, KeyVault, ASB, DB
│  │  │  └─ terraform/             # alternatywnie TF
│  └─ pipelines/                   # szablony GitHub Actions (reusable workflows)
│
├─ ops/
│  ├─ sre/
│  │  ├─ slo.yml
│  │  ├─ runbook.md
│  │  └─ postmortem-template.md
│  ├─ security/
│  │  ├─ threat-model.md
│  │  └─ data-handling.md          # PII/GDPR/retencja, roles, logging policy
│  └─ finops/
│     └─ cost-model.md
│
├─ docs/
│  ├─ architecture.md              # C4 overview
│  ├─ adr/                         # Architecture Decision Records
│  ├─ roadmap.md
│  └─ 90-day-plan.md               # artefakt EM/Head
│
├─ env/
│  ├─ .env.example
│  └─ secrets.example/             # placeholdery; real secrets → Key Vault
│
├─ tools/
│  ├─ scripts/                     # make.ps1/make.sh: build, test, run, seed
│  ├─ hooks/                       # git hooks (pre-commit itp.)
│  └─ ci-templates/
│
└─ infra/local/seed/               # dane przykładowe
