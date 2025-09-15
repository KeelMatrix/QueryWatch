> The project is currently under development. Keep an eye out for its release!

# KeelMatrix.QueryWatch

> Catch N+1 queries and slow SQL in tests. Fail builds when query budgets are exceeded.

[![Build](https://github.com/OWNER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/OWNER/REPO/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/KeelMatrix.QueryWatch.svg)](https://www.nuget.org/packages/KeelMatrix.QueryWatch/)

## Install

```bash
dotnet add package KeelMatrix.QueryWatch
```

## 5‑minute success (with JSON for CI)

**Per‑test scope → export JSON:**

```csharp
using KeelMatrix.QueryWatch.Testing;

// JSON is written even if assertions fail (helps CI).
using var q = QueryWatchScope.Start(
    maxQueries: 5,
    maxAverage: TimeSpan.FromMilliseconds(50),
    exportJsonPath: "artifacts/qwatch.report.json");

// wire EF Core or ADO to q.Session, run your code...
```

**Gate in CI (already wired in ci.yml):**

```pwsh
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --max-queries 50
```

**EF Core:** see `src/KeelMatrix.QueryWatch/README.md` for full example.

## Why QueryWatch?

- **Prevents N+1 and slow queries** before they reach production.
- **Lightweight**: plug into EF Core or wrap ADO/Dapper connection.
- **Redaction hooks**: mask PII or noisy literals.
- **CI‑friendly**: export JSON and use the CLI gate to fail PRs.

## JSON schema

Stable, compact summary emitted by `KeelMatrix.QueryWatch.Reporting.QueryWatchJson.ExportToFile(report, path)` with fields:
`schema, startedAt, stoppedAt, totalQueries, totalDurationMs, averageDurationMs, events[]`.

## CLI

- `--input` path to JSON (default `artifacts/qwatch.report.json`)
- `--max-queries`, `--max-average-ms`, `--max-total-ms`
- `--baseline <file>` and `--write-baseline` to store today’s good results

## License

MIT. See `LICENSE`.  See `PRIVACY.md` for telemetry stance (off by default).
