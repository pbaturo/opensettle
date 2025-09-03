# Redpanda (Kafka-compatible) — Local Development Guide

**Purpose:** Provide a fast, Kafka-compatible broker for OpenSettle local development and tests.  
**Scope:** Single-node Redpanda via Docker Compose, topics for payments, headers & connectivity rules, basic ops, and troubleshooting.

---

## Why Redpanda here?

- **Kafka API compatible**: works with `Confluent.Kafka` client you’ll use in .NET.
- **Single binary**: zero ZooKeeper, quick startup for dev.
- **Drop-in**: easy to swap later for Confluent Cloud or Azure Event Hubs (Kafka API) without changing producer/consumer code.

> ⚠️ Local setup is **PLAINTEXT** (no TLS/SASL). For production, enable auth + network isolation.

---

## Topology & Ports

We run Redpanda with **dual listeners** so both **host apps** and **containers** can connect:

- **Host → Redpanda:** `localhost:9092` (advertised EXTERNAL)
- **Container → Redpanda:** `redpanda:29092` (advertised INTERNAL on compose network)
- **Admin API:** `localhost:9644`

---

## Start / Stop

**Compose file:** `infra/compose/docker-compose.yml` (includes Redpanda, Postgres, Redis, Jaeger)

```bash
# Up all infra (broker, db, cache, tracing)
docker compose -f infra/compose/docker-compose.yml up -d

# Check cluster status
docker exec -it redpanda rpk cluster info

# Down (keep volumes)
docker compose -f infra/compose/docker-compose.yml down
```

**Wipe Redpanda data (dev only):**
```bash
docker compose -f infra/compose/docker-compose.yml down -v   # drops volumes incl. topics
```

---

## Required Topics

Create once (or script in Makefile/CI):

```bash
docker exec -it redpanda rpk topic create payments.created -p 3 -r 1
docker exec -it redpanda rpk topic create payments.created.dlq -p 3 -r 1
```

**Guidelines**
- `payments.created`: start with **3 partitions** for parallelism; key by **paymentId** to preserve per-payment order.
- `payments.created.dlq`: same partitions; used for poison messages (park with a reason and `traceId` in headers).

> Retention/compaction can be adjusted later; keep defaults for now.

---

## App Configuration (host apps)

Put in `appsettings.Development.json` or env vars:

```ini
Kafka__BootstrapServers=localhost:9092
Kafka__Topics__PaymentsCreated=payments.created
Kafka__Topics__PaymentsCreatedDlq=payments.created.dlq
Kafka__SchemaVersion=v1
```

If your service runs **inside the same compose** project/network, use:
```
Kafka__BootstrapServers=redpanda:29092
```

---

## Headers you MUST send with messages

- `traceparent` (W3C)
- `tracestate` (optional)
- `correlation-id` (business correlation)
- `idempotency-key` (end-to-end dedupe)
- `tenant-id` (optional if single tenant)
- `schema-version` (e.g., `v1`)

> Consumers validate these and **commit after success**. Replays are expected; handlers must be **idempotent**.

---

## Quick rpk Cheatsheet

**List topics**
```bash
docker exec -it redpanda rpk topic list
```

**Produce a test message**
```bash
docker exec -it redpanda rpk topic produce payments.created \
  -k "payment-123" \
  -H "traceparent:00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" \
  -H "correlation-id:demo-1" \
  -H "idempotency-key:demo-1" \
  -H "schema-version:v1"
# paste JSON payload and press Ctrl+D to send
{"paymentId":"payment-123","customerId":"cust-42","amount":{"amount":"12.34","currency":"PLN"},"method":"card","status":"created","createdAt":"2025-09-03T10:00:00Z"}
```

**Consume**
```bash
docker exec -it redpanda rpk topic consume payments.created -n 1 -o start
```

**Describe topic**
```bash
docker exec -it redpanda rpk topic describe payments.created
```

---

## Observability

- **Tracing**: producers add `traceparent` in headers; consumers **resume** the trace and emit spans.
- **Jaeger UI**: http://localhost:16686
- **OTLP endpoint**: `http://localhost:4317` (gRPC) / `http://localhost:4318` (HTTP) — wire up via OpenTelemetry SDK in your .NET services.

---

## DLQ (Dead Letter Queue) Policy

- **When**: after bounded retries/backoff on non-transient errors.
- **How**: publish to `payments.created.dlq` with original key, payload, and extra headers:
  - `error.reason` (short machine code)
  - `error.first-seen` (ISO-8601)
  - keep original `traceparent`, `correlation-id`, `idempotency-key`

**Operator flow**
1. Investigate with `traceId` in APM/Jaeger.
2. Fix root cause or payload.
3. **Re-enqueue** to the primary topic (or a repair topic) when safe.

---

## Best-practice Defaults (dev)

**Producer**
- `EnableIdempotence=true`, `Acks=All`
- Modest `LingerMs`/batching for throughput
- Retries with jittered backoff

**Consumer**
- `EnableAutoCommit=false` (manual commit **after** success)
- `AutoOffsetReset=Earliest` (dev convenience)
- Cooperative rebalancing if supported
- Reasonable `MaxPollIntervalMs`, `SessionTimeoutMs`
- Retry transient, DLQ on poison

---

## Common Pitfalls & Fixes

**❌ Can’t connect from host**  
- Ensure `--advertise-kafka-addr EXTERNAL://127.0.0.1:9092` is set (compose already does).  
- Verify nothing else is bound to `9092` (`lsof -i :9092` / change port if needed).

**❌ Can’t connect from another container**  
- Use `redpanda:29092`. The compose file advertises INTERNAL for that DNS name.

**❌ Messages lost after restart**  
- Check you didn’t run `down -v`. Use named volume `redpanda-data` to persist.

**❌ Duplicates during testing**  
- Expected with at-least-once. Confirm idempotency store is working and **commit after success**.

**❌ Offsets not moving**  
- You likely enabled auto-commit but still commit late, or you never call manual commit. Decide one approach (we use **manual** commits).

---

## FAQ

**Q: Why not exactly-once?**  
A: Hard across multiple services. We choose **at-least-once** + idempotent handlers + outbox to keep guarantees clear and operations simple.

**Q: Can we switch to Confluent Cloud / Event Hubs?**  
A: Yes. Keep the same producer/consumer code; change `BootstrapServers` and security settings (SASL/TLS). Keep headers and idempotency logic identical.

**Q: How many partitions?**  
A: Start with 3 for dev. In prod, size by throughput, key distribution, and consumer parallelism. Remember: ordering is **per partition** (key by `paymentId`).

---
