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

## Redactor ordering tips

If you use multiple redactors, **order matters**. A safe, effective default is:

1. **Whitespace normalizer** – make SQL text stable across environments/providers.
2. **High‑entropy token masks** – long hex tokens, JWTs, API keys.
3. **PII masks** – emails, phone numbers, IPs.
4. **Custom rules** – your app–specific patterns (use `AddRegexRedactor(...)`).

> Put *broad* rules (like whitespace) first, and *specific* rules (like PII) after. This lowers the chance one rule prevents another from matching.

## Typical budgets for Dapper‑heavy solutions

Dapper often issues *fewer, more targeted* commands than ORMs. Reasonable starting points (tune per project):

- **End‑to‑end web test:** `--max-queries 40`, `--max-average-ms 50`, `--max-total-ms 1500`.
- **Repository‑level test:** `--max-queries 10`, `--max-average-ms 25`, `--max-total-ms 250`.
- **Per‑pattern budgets:** cap hot spots explicitly, e.g.:

  ```
  --budget "SELECT * FROM Users*=5" --budget "regex:^UPDATE Orders SET=3"
  ```

- Increase `sampleTop` in code (`QueryWatchScope.Start(..., sampleTop: 200)`) if you rely on many per‑pattern budgets.

Treat these as **guardrails**: keep design flexible but catch accidental N+1s or slow queries early.
