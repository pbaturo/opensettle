<!-- docs/observability/tracing-logging.md -->
# Observability — Tracing & Logging (HTTP & Kafka)

## HTTP
- **Propagate** W3C headers: `traceparent`, `tracestate`
- **Respond** with header: `trace-id: <traceId>`
- **Errors** include `traceId` in body

**Structured log fields (all lines)**
- `TraceId`, `SpanId`, `RouteTemplate`, `ApiVersion`
- If money present: `money.currency`, `money.amount_string`

**Sampling**
- Always sample error traces
- Sample a small % of successful requests

## Kafka
- **Headers**: `traceparent`, `tracestate`, `correlation-id`, `tenant-id`, `schema-version`
- **Key**: choose stable key for ordering (e.g., `paymentId`)
- **Consumer** resumes trace from headers

## Runbook (400 money errors)
1. Locate trace via `traceId`
2. Confirm validator reason (`code`)
3. If client-side bug: point to contract (`ADR-0001`); if server config: adjust limits
4. Track error rate; alert if exceeds threshold
