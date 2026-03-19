# KeelMatrix.QueryWatch

Core package for recording SQL executed during tests and enforcing database-query budgets.

Use this package when you want to:
- catch N+1 regressions
- assert query counts or timings in tests
- export JSON summaries for CI gates
- instrument ADO.NET or Dapper code without adding a production profiler

## Install

```bash
dotnet add package KeelMatrix.QueryWatch
```

## Quick Example

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Assertions;
using KeelMatrix.QueryWatch.Reporting;

using QueryWatchSession session = new();

using var conn = rawConnection.WithQueryWatch(session);

// Run data access code here.

QueryWatchReport report = session.Complete();

report.ShouldHaveExecutedAtMost(20);
report.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(15));

QueryWatchJson.ExportToFile(report, "artifacts/qwatch.json", sampleTop: 200);
```

## What This Package Includes

- `QueryWatchSession` for collecting events
- `WithQueryWatch(...)` wrappers for ADO.NET and Dapper-style usage
- fluent-style assertions under `KeelMatrix.QueryWatch.Assertions`
- JSON export helpers under `KeelMatrix.QueryWatch.Reporting`

## Typical Use Cases

- Guard a repository or service test against query-count explosions
- Fail CI if the average SQL duration for a test workload exceeds budget
- Export summaries for baseline comparison in the CLI
- Capture parameter shape metadata without storing actual values

## Related Packages

- EF Core integration: `KeelMatrix.QueryWatch.EfCore`
- Query redaction helpers: `KeelMatrix.QueryWatch.Redaction`
- JSON contracts: `KeelMatrix.QueryWatch.Contracts`

## Documentation

Full docs and examples live in the repo root:

- [Root README](https://github.com/KeelMatrix/QueryWatch#readme)
- [EF Core wiring](https://github.com/KeelMatrix/QueryWatch#ef-core-wiring)
- [Dapper wiring](https://github.com/KeelMatrix/QueryWatch#dapper-wiring)
- [CLI usage](https://github.com/KeelMatrix/QueryWatch#cli)
