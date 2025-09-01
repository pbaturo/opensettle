<!-- docs/adr/0002-api-versioning-strategy.md -->
# ADR-0002 — API versioning strategy (route-based, major versions)

**Status:** Accepted  
**Date:** 2025-09-01  
**Owner:** Paweł Baturo

---

## Context
OpenSettle exposes public/partner APIs that evolve over time. We must deliver new capabilities without breaking existing clients, provide clear deprecation signals, and support safe rollouts/rollbacks with observability.

## Decision
Use **route-based major versioning** for HTTP APIs (e.g., `/v1/...`, `/v2/...`).  
Within a major version, only **backward-compatible (additive)** changes are allowed. Any breaking change requires a **new major version route**.

## Rules
- **Canonical version location:** URL path prefix: `/v{major}` (e.g., `/v1/payments`).
- **Within vN:** additive-only (new endpoints, new optional fields, defaultable behavior).  
  No removals/renames/behavioral breaks.
- **Breaking change:** publish `/v{N+1}`; run vN and vN+1 in parallel through a deprecation window.
- **Unknown/retired version:** return `404 Not Found` (or `410 Gone` after sunset) with a stable machine error code.

## Deprecation & Sunset Policy
- Announce deprecations via:
  - Response headers on vN:
    - `Deprecation: true`
    - `Sunset: <RFC 1123 date>`
    - `Link: <https://docs.opensettle.dev/deprecations/v1>; rel="deprecation"`
  - Changelog & partner notification channel.
- Operate **vN and vN+1 in parallel** for the deprecation window (default: **180 days**, service may adjust with approval).
- After the window, remove vN endpoints; keep archived docs for reference.

## Back/Forward Compatibility Tactics
- For payload changes (e.g., Money shape), use **dual-read, single-write** during rollout:
  - **Readers** accept both old and new payloads.
  - **Writers** emit only the new payload.
- Provide SDKs/examples for vN+1.
- Expose per-version metrics (traffic share, error rate, parse failures) to track adoption and trigger alerts.

## Error Contract (unsupported/bad version)
~~~json
{
  "code": "api.unsupported_version",
  "message": "This API version is not supported. Please use /v2.",
  "traceId": "<w3c-trace-id>"
}
~~~

## Observability
- **Tracing:** propagate W3C headers (`traceparent`, `tracestate`); return `trace-id` response header; include `traceId` in error bodies.
- **Structured logs:** include `api.version`, `route.template`, `traceId`, `spanId`.  
- **Dashboards:** show per-version request volume, error rates, latency; alert when vN traffic falls below target thresholds ahead of sunset.

## Security & Governance
- Apply the **same authN/Z, quotas, and rate limits** across versions unless explicitly documented.  
- Maintain **contract tests** for vN to catch accidental breaking changes.  
- Versioned endpoints must share the **same privacy/logging policy** (no raw body logging).

## Alternatives Considered
1. **Header/media-type versioning** (e.g., `Accept: application/vnd.opensettle+json;v=2`)  
   Pros: avoids route sprawl; flexible.  
   Cons: less visible to humans/caches/gateways; harder for public partners.
2. **No versioning** (continuous)  
   Pros: simple in theory.  
   Cons: unsafe for public APIs; difficult to manage breaking changes.

## Migration Playbook
- Phase 0: ship v2 behind a flag; run **synthetic traffic** in staging.
- Phase 1: **Canary** 1–5% of production clients (or selected partners) to v2.
- Phase 2: **Dual-read** validators accept both v1/v2 payloads; **single-write** emits v2 only.
- Phase 3: Announce **Sunset** date for v1; publish SDK/guide.
- Phase 4: Ramp to 100% v2; monitor metrics (parse failures, error rates).
- Phase 5: Remove v1 after the window; keep archived docs.

## Test Strategy (to implement)
- `v1_and_v2_endpoints_operate_in_parallel`
- `v1_responses_include_deprecation_and_sunset_headers`
- `unknown_version_returns_api_unsupported_version_error`
- `dual_read_validates_old_and_new_payload_shapes`
- `per_version_metrics_visible_in_dashboards`


