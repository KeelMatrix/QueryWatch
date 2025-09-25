 > The project is currently under development. Keep an eye out for its release!

# KeelMatrix.QueryWatch

> Catch N+1 queries and slow SQL in tests. Fail builds when query budgets are exceeded.

[![Build](https://github.com/OWNER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/OWNER/REPO/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/KeelMatrix.QueryWatch.svg)](https://www.nuget.org/packages/KeelMatrix.QueryWatch/)

## Install

```bash
dotnet add package KeelMatrix.QueryWatch
# EF Core users:
dotnet add package KeelMatrix.QueryWatch.EfCore
```

## 5‑minute success (with JSON for CI)

**Per‑test scope → export JSON:**

```csharp
using KeelMatrix.QueryWatch.Testing;

// JSON is written even if assertions fail (helps CI).
using var q = QueryWatchScope.Start(
    maxQueries: 5,
    maxAverage: TimeSpan.FromMilliseconds(50),
    exportJsonPath: "artifacts/qwatch.report.json",
    sampleTop: 50); // increase if you plan to use per‑pattern budgets in CLI
```

**Gate in CI (already wired in ci.yml):**

```pwsh
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --max-queries 50
```

## EF Core wiring

```csharp
using KeelMatrix.QueryWatch.EfCore; // extension lives in the EfCore adapter package
using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<MyDbContext>()
    .UseSqlite("Data Source=:memory:")
    .UseQueryWatch(q.Session)    // adds the interceptor
    .Options;
```

## CLI

```
--input <file>               (repeatable) JSON summary exported by QueryWatch
--max-queries N              Fail if total queries exceed N
--max-average-ms MS          Fail if average duration (ms) exceeds MS
--max-total-ms MS            Fail if total duration (ms) exceeds MS
--baseline <file>            Compare against a baseline summary file
--baseline-allow-percent P   Allow +P% regression vs baseline before failing
--write-baseline             Write current aggregated summary to --baseline
--budget "<pattern>=<max>"   Per‑pattern query count budget (repeatable). Pattern
                             supports wildcards (*, ?) or prefix with 'regex:'.
```

### Multi‑file support

Repeat `--input` to aggregate multiple JSONs (e.g., per‑project reports in a mono‑repo). Budgets evaluate on the aggregate.

### GitHub PR summary

When run inside GitHub Actions, the CLI writes a Markdown table to the **Step Summary** automatically, so reviewers see metrics and any violations at a glance.

### Note on per‑pattern budgets

Budgets match against the `events` captured in the JSON file(s). These are the top‑N slowest events by duration to keep files small. If you want strict coverage, export with a higher `sampleTop` in `QueryWatchJson.ExportToFile`, or pass a larger `sampleTop` to `QueryWatchScope.Start(...)`.
