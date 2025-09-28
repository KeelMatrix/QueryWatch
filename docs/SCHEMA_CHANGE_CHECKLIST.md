# QueryWatch JSON Schema — Change Checklist

**Purpose:** keep the wire format stable for CI tools and downstream consumers.

When changing the schema:
1. **Prefer additive changes**. Do not remove/rename fields; add new fields with safe defaults.
2. **Bump `schema` version** in both writer and contract defaults.
3. **Update golden files** under `tests/KeelMatrix.QueryWatch.Tests/Fixtures/`.
4. **Update the source-gen context** (`QueryWatchJsonContext`) if new types are introduced.
5. **Add/adjust round-trip tests** to cover new fields.
6. **Run perf checks** (deserialize many summaries) and compare with previous commit.
7. **Announce in CHANGELOG** and communicate deprecation timelines if needed.
