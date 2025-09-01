<!-- docs/adr/0001-money-wire-format.md -->
# ADR-0001 — Money wire format (amount-as-string JSON)

**Status:** Accepted  
**Date:** 2025-09-01  
**Owner:** Paweł Baturo

---

## Context
OpenSettle services exchange monetary values across heterogeneous clients (JS/TS, .NET, Java, Python). IEEE-754 floating point in many stacks can lose cent precision if amounts are transported as JSON numbers. We need a single, durable wire format that is precise across languages, simple to validate, culture-safe, and evolvable.

## Decision
Adopt **JSON object with amount represented as a string** in invariant format.

**Canonical shape**
~~~json
{ "amount": "12.34", "currency": "PLN" }
~~~

## Rules & Constraints
- `currency`: exactly **3 ASCII letters**, **uppercase** (e.g., `"PLN"`, `"EUR"`).
- `amount`: **string**, **InvariantCulture**, exactly **two decimal places** (`^\d+\.\d{2}$`).
- Values are **non-negative** in v1.
- Guardrail range (configurable): `0.00` – `9,999,999,999.99`.
- **Not allowed:** scientific notation, grouping separators, plus sign, or whitespace.

## Culture Policy
- APIs **emit & accept InvariantCulture** only.
- UI layers handle localisation separately for display (e.g., `pl-PL` formatting).

## Alternatives Considered
1. **Minor units (integer cents)**  
   Pros: language-agnostic, exact.  
   Cons: less human-readable; currencies with non-2dp scales complicate clients.
2. **Decimal number** (e.g., `{ "amount": 12.34 }`)  
   Pros: simplest shape.  
   Cons: risk of double coercion/precision loss in some stacks (esp. JS).
3. **String “12.34 PLN”**  
   Pros: human-friendly.  
   Cons: parsing ambiguity; harder machine validation/versioning.

## Consequences
- **Precision safety** across languages.
- Minor consumer cost (string → decimal parse).
- Validation remains simple and consistent across services.

## Compatibility & Versioning
- Breaking changes require a new **route version** (see ADR-0002).  
- Use **dual-read, single-write** during migrations:
  - Readers accept old and new shapes for a deprecation window.
  - Writers emit only the new shape.
- Announce deprecation, track adoption via metrics, remove old readers after the window.
- If currency-specific scales (e.g., JPY 0dp, TND 3dp) are needed later, publish a new version with a migration guide.

## Validation (Ingress)
Reject if:
- `currency` not `[A-Z]{3}` (ASCII), or
- `amount` not `^\d+\.\d{2}$`, or
- value out of configured range.

**Error contract (HTTP 400)**
~~~json
{
  "code": "money.invalid_amount_format",
  "message": "Amount must be a string with two decimal places (InvariantCulture).",
  "field": "amount",
  "traceId": "<w3c-trace-id>"
}
~~~

## Observability
- Propagate W3C tracing (`traceparent`, `tracestate`); return `trace-id` header; include `traceId` in error bodies.
- Structured logs: `money.currency`, `money.amount_string`; never log raw request bodies.
- Sampling: always sample error traces; low-rate sampling for successful requests.

## Security & Privacy
Money values aren’t PII, but request bodies may include PII—**do not** log raw payloads. `trace-id` must be opaque (no business data).

## Test Strategy (to implement)
- `json_roundtrips_exactly_under_chosen_contract`
- `rejects_non_ascii_currency`
- `rejects_more_than_two_decimals_or_sci_notation`
- `rejects_out_of_range_amount`
- `errors_include_traceId_and_stable_code`
- (if migrating) `dual_read_accepts_old_and_new_shapes`

## Implementation Notes
- Provide shared validators/DTOs to keep rules consistent across services.
- Keep machine format (object) and human previews strictly separate.

