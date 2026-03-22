# QueryWatch

QueryWatch is a .NET library for catching database-query regressions in tests and CI before they reach production. It records executed SQL, counts queries, measures timings, exports JSON summaries, and lets you fail builds on budget violations.

Works with:
- ADO.NET
- Dapper
- EF Core

## Why Use It

QueryWatch is designed for test-time guardrails, not production profiling dashboards.

Typical use cases:
- Catch N+1 regressions introduced by ORM changes
- Enforce per-test query-count budgets
- Fail CI when average or total SQL time drifts upward
- Export machine-readable summaries for baselines and PR reporting
- Capture parameter shape metadata without storing parameter values

## Packages

| Package | Purpose |
| --- | --- |
| `KeelMatrix.QueryWatch` | Core recording, assertions, JSON export, ADO.NET and Dapper wrapping |
| `KeelMatrix.QueryWatch.EfCore` | EF Core interceptor and `UseQueryWatch(...)` integration |

## Install

Core only:

```bash
dotnet add package KeelMatrix.QueryWatch
```

EF Core integration:

```bash
dotnet add package KeelMatrix.QueryWatch
dotnet add package KeelMatrix.QueryWatch.EfCore
```

Optional redaction helpers:

```bash
dotnet add package KeelMatrix.Redaction
```

Local development restores `KeelMatrix.Redaction` and `KeelMatrix.Telemetry` from the repo feed at `./artifacts/packages`. Stage those packages from their sibling repos before a fresh QueryWatch restore/build.

Until `KeelMatrix.Redaction` and `KeelMatrix.Telemetry` are published to NuGet.org, clean CI must bootstrap them into `./artifacts/packages` before any QueryWatch restore/build/test/pack step. Local development can rely on sibling checkouts, while CI uses `QW_REDACTION_REPO_ROOT` and `QW_TELEMETRY_REPO_ROOT` to point the bootstrap scripts at checked-out dependency repos.

## 5-Minute Quick Start

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Assertions;
using KeelMatrix.QueryWatch.Reporting;

using QueryWatchSession session = new();

using var conn = rawConnection.WithQueryWatch(session);

// Run code that talks to the database.

QueryWatchReport report = session.Complete();

report.ShouldHaveExecutedAtMost(20);
report.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(15));

QueryWatchJson.ExportToFile(report, "artifacts/qwatch.report.json", sampleTop: 200);
```

The exported JSON can be consumed by the CLI in CI.

## Real-World Scenarios

### Prevent accidental N+1 queries

Wrap the test scope, execute the application code, and assert the query count stays below a fixed threshold.

### Gate pull requests on SQL budgets

Export a summary file during tests, then run the CLI in GitHub Actions to fail the build if query counts or timings regress.

### Track parameter shape safely

Enable parameter-shape capture to understand whether code is issuing parameterized commands, without persisting sensitive parameter values.

### Normalize SQL before comparisons

Add redactors to remove secrets, GUID noise, timestamps, or tokens so CI diffs focus on structural query changes.

## Quick Start - Samples (Local)

This repo ships three sample apps that consume local packages built from source.
The bootstrap scripts resolve `KeelMatrix.Redaction` and `KeelMatrix.Telemetry` from sibling checkouts under `../../KeelMatrix.Redaction/app` and `../../KeelMatrix.Telemetry/app` by default, or from `QW_REDACTION_REPO_ROOT` and `QW_TELEMETRY_REPO_ROOT` when CI overrides those locations.

1. Build and pack the local packages used by the samples:
   - PowerShell: `pwsh -NoProfile -File build/Dev-PackInstallSamples.ps1`
   - bash: `bash build/Dev-PackInstallSamples.sh`

2. Run a sample:

   ```bash
   dotnet run --project ./samples/EFCore.Sqlite/EFCore.Sqlite.csproj -c Release
   ```

4. Gate the generated summary with the CLI:

   ```bash
   dotnet run --project ./tools/KeelMatrix.QueryWatch.Cli -- --input ./samples/EFCore.Sqlite/bin/Release/net8.0/artifacts/qwatch.ef.json --max-queries 50
   ```

## EF Core Wiring

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.EfCore;
using KeelMatrix.QueryWatch.Reporting;

using var session = new QueryWatchSession();

var options = new DbContextOptionsBuilder<MyDbContext>()
    .UseSqlite("Data Source=:memory:")
    .UseQueryWatch(session)
    .Options;

// Run workload...

var report = session.Complete();
QueryWatchJson.ExportToFile(report, "artifacts/ef.json", sampleTop: 200);
```

The EF Core interceptor records executed commands only. Use `QueryWatchOptions` to tune SQL text capture, sampling, and parameter-shape capture.

## Dapper Wiring

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Reporting;

using var session = new QueryWatchSession();

await using var raw = new SqliteConnection("Data Source=:memory:");
await raw.OpenAsync();

using var conn = raw.WithQueryWatch(session);
var rows = await conn.QueryAsync("SELECT 1");

var report = session.Complete();
QueryWatchJson.ExportToFile(report, "artifacts/dapper.json", sampleTop: 200);
```

If the underlying connection is a `DbConnection`, QueryWatch uses the higher-fidelity ADO.NET wrapper automatically.

## ADO.NET Wiring

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Reporting;

using var session = new QueryWatchSession();

await using var raw = new SqliteConnection("Data Source=:memory:");
await raw.OpenAsync();

using var conn = raw.WithQueryWatch(session);
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT 1";
await cmd.ExecuteNonQueryAsync();

var report = session.Complete();
QueryWatchJson.ExportToFile(report, "artifacts/ado.json", sampleTop: 200);
```

## Redaction

If captured SQL can include secrets, tokens, email addresses, or noisy identifiers, add `KeelMatrix.Redaction` and configure redactors on `QueryWatchOptions`.

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Redaction;
using KeelMatrix.Redaction;

var options = new QueryWatchOptions()
    .UseRecommendedRedactors(includeTimestamps: false, includeIpAddresses: false, includePhone: false);
```

## Budgets

At test time, enforce budgets directly on `QueryWatchReport`.

At CI time, use the CLI for:
- total-query budgets
- average-duration budgets
- total-duration budgets
- baseline comparisons
- per-pattern budgets

Per-pattern budgets support wildcards (`*`, `?`) or a `regex:` prefix.

Examples:

```bash
--budget "SELECT * FROM Users*=1"
--budget "regex:^UPDATE Orders SET=3"
```

If a summary is top-N sampled, budgets are evaluated only over those captured events. Increase `sampleTop` if you need stricter guarantees.

## CLI

<!-- BEGIN:CLI_FLAGS -->
```
--input <path>               Input JSON summary file. (repeatable)
--max-queries N              Fail if total query count exceeds N.
--max-average-ms N           Fail if average duration exceeds N ms.
--max-total-ms N             Fail if total duration exceeds N ms.
--baseline <path>            Baseline summary JSON to compare against.
--baseline-allow-percent P   Allow +P% regression vs baseline before failing.
--write-baseline             Write current aggregated summary to --baseline.
--budget "<pattern>=<max>"   Per-pattern query count budget (repeatable). (repeatable)
                             Pattern supports wildcards (*, ?) or prefix with 'regex:' for raw regex.
--require-full-events        Fail if input summaries are top-N sampled.
--help                       Show this help.
```

<!-- END:CLI_FLAGS -->

Multi-file support:
- repeat `--input` to aggregate summaries from multiple test projects
- compare current results against a baseline summary
- write GitHub Actions step summaries automatically when running in CI

## Troubleshooting

- Pattern budgets look incomplete: your summary may be sampled too aggressively. Re-export with a higher `sampleTop`.
- Baseline checks are noisy: use `--baseline-allow-percent` and keep baselines representative.
- CLI flags in the README look stale: refresh the generated block with `build/Update-ReadmeFlags.ps1`.
- You do not want SQL text on a hot path: set `QueryWatchOptions.CaptureSqlText = false`.
- You want metadata without secret leakage: use parameter-shape capture, not parameter-value capture.

## Privacy

QueryWatch uses `KeelMatrix.Telemetry` transitively for minimal anonymous usage telemetry.

See:
- [PRIVACY.md](PRIVACY.md) for the QueryWatch-specific summary
- [KeelMatrix.Telemetry README](https://github.com/KeelMatrix/Telemetry#readme) for the maintained telemetry behavior and opt-out details

## License

MIT
