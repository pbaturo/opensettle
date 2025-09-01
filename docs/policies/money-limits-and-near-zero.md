<!-- docs/policies/money-limits-and-near-zero.md -->
# Policy — Limits & Near-Zero

- **Limits**: default guardrail `0.00` .. `9,999,999,999.99` (service-configurable)
- **Near-zero**: *Policy A (strict)* — if raw result is `< 0`, throw; do not “round into” zero
- **Non-2dp currencies**: not supported in v1; future version will introduce per-currency scales
