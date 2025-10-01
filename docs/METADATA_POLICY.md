# QueryWatch — Event Metadata Policy

This document explains how event-level metadata is attached across adapters (ADO.NET, Dapper, EF Core) to make results predictable and CI-friendly.

## Parameter shape capture (privacy-preserving)

- Flag: `QueryWatchOptions.CaptureParameterShape` (top-level).
- When `true`, adapters attach **parameter shape** only — _names, DB types, CLR types, directions_ — never values.
- JSON shape is emitted under `event.meta.parameters`, e.g.:

```json
{
  "meta": {
    "parameters": [
      { "name": "@id", "dbType": "Int32", "clrType": "System.Int32", "direction": "Input" }
    ]
  }
}
```

- Supported adapters:
  - **ADO.NET wrapper** — captured from `DbCommand.Parameters`.
  - **EF Core interceptor** — captured from the underlying `DbCommand.Parameters` raised in EF events.

## Normalized failure envelope

On exceptions, all adapters emit the same, minimal keys:

```json
{ "failed": true, "exception": "System.InvalidOperationException", "provider": "<ado|dapper|efcore>" }
```

This envelope is additive: adapters may add more keys in the future, but these three are stable.

## Per-adapter text capture toggles (fast path)

In addition to `QueryWatchOptions.CaptureSqlText` (global), you can turn off text capture per adapter:

- `DisableAdoTextCapture`
- `DisableDapperTextCapture`
- `DisableEfCoreTextCapture`

When disabled, the adapter records an empty `CommandText` regardless of the global flag, avoiding redaction work on hot paths.
