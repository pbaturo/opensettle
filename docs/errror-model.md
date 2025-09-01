<!-- docs/error-model.md -->
# Error Model — Money Validation

**Principles**
- Stable, machine-readable `code`
- Clear `message` (no internals/PII)
- Always include `traceId`
- HTTP **400 Bad Request** for validation

| code                         | http | field    | message                                                                     |
|-----------------------------|------|----------|-----------------------------------------------------------------------------|
| money.invalid_currency      | 400  | currency | Currency must be 3 uppercase ASCII letters (ISO-like).                     |
| money.invalid_amount_format | 400  | amount   | Amount must be a string with two decimal places (InvariantCulture).        |
| money.out_of_range          | 400  | amount   | Amount is outside the allowed range.                                       |
| money.negative_not_allowed  | 400  | amount   | Negative amounts are not allowed in v1.                                    |
| money.unsupported_currency  | 400  | currency | Currency not supported by this endpoint.                                   |

**Response shape**
```json
{
  "code": "money.invalid_amount_format",
  "message": "Amount must be a string with two decimal places (InvariantCulture).",
  "field": "amount",
  "traceId": "<w3c-trace-id>"
}
```

**Logging/Tracing**
- Log `money.currency`, `money.amount_string`, `reason=<code>`, `traceId`
- Do **not** log raw request bodies
