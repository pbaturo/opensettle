<!-- docs/tests/test-plan-money-wire.md -->
# Test Plan — Money Wire Contract

## Round-trip
- `json_roundtrips_exactly_under_chosen_contract`
- `large_amount_within_limits_roundtrips`

## Validation (rejects)
- `rejects_non_ascii_currency`
- `rejects_bad_amount_format_two_dp_required`
- `rejects_scientific_notation_and_whitespace`
- `rejects_out_of_range_amount`
- `rejects_negative_amount_in_v1`

## Error contract
- `bad_money_returns_400_with_machine_code_and_traceId`
- `logs_include_reason_without_raw_payload`

## Compatibility (if migrating later)
- `dual_read_accepts_old_and_new_shapes`
