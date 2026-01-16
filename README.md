# QueryWatch

Guardrail your **database queries** in tests & CI. Capture executed SQL, enforce **budgets** (counts & timings), and fail builds on regressions. Works with **ADO.NET**, **Dapper**, and **EF Core**.

**Quick links:**  
- 👉 [Quick Start — Samples (local)](#quick-start--samples-local)  
- 👉 [EF Core wiring](#ef-core-wiring) · [Dapper wiring](#dapper-wiring) · [ADO.NET wiring](#adonet-wiring)  
- 👉 [CLI flags](#cli) · [Troubleshooting](#troubleshooting)  

---

## 5-minute success (tests)

Use a `QueryWatchSession`, then export JSON and assert budgets directly.

``` csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Assertions;
using KeelMatrix.QueryWatch.Reporting;

using QueryWatchSession session = new();

// Wrap your connection so commands are recorded
using var conn = rawConnection.WithQueryWatch(session);

// ... run your code that hits the DB ...

// Finalize session
QueryWatchReport report = session.Complete();

// Export JSON for the CLI
QueryWatchJson.ExportToFile(report, "artifacts/qwatch.report.json", sampleTop: 200);

// Enforce budgets in tests
report.ShouldHaveExecutedAtMost(50);
report.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(25));
report.ShouldHaveMaxTotalTime(TimeSpan.FromMilliseconds(30));
```

The file `artifacts/qwatch.report.json` is now ready for the CLI gate.

---

## Quick Start — Samples (local)

This repo ships three tiny sample apps (EF Core, ADO.NET, Dapper) that **consume local packages** you build from source.

1. **Pack the libraries** (run **at repo root**):
   ```bash
   dotnet pack ./src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj -c Release --include-symbols --p:SymbolPackageFormat=snupkg --output ./artifacts/packages
   dotnet pack ./src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj -c Release --include-symbols --p:SymbolPackageFormat=snupkg --output ./artifacts/packages
   dotnet pack ./src/KeelMatrix.QueryWatch.Redaction/KeelMatrix.QueryWatch.Redaction.csproj -c Release --include-symbols --p:SymbolPackageFormat=snupkg --output ./artifacts/packages
   ```
2. **Install local packages to samples** (pins to `../artifacts/packages` via `samples/NuGet.config`):
   - Windows (PowerShell): `pwsh -NoProfile -File build/Dev-PackInstallSamples.ps1`  
   - Linux/macOS (bash): `bash build/Dev-PackInstallSamples.sh`
3. **Run a sample** (EF example shown):
   ```bash
   dotnet run --project ./samples/EFCore.Sqlite/EFCore.Sqlite.csproj -c Release
   ```
4. **Gate with the CLI**:
   ```bash
   dotnet run --project ./tools/KeelMatrix.QueryWatch.Cli -- --input ./samples/EFCore.Sqlite/bin/Release/net8.0/artifacts/qwatch.ef.json --max-queries 50
   ```

> CI uses the same flow and restores using `samples/NuGet.config` so **samples build after `dotnet pack` with no tweaks**.

---

## EF Core wiring

```csharp
using Microsoft.EntityFrameworkCore;
using KeelMatrix.QueryWatch.EfCore;
using KeelMatrix.QueryWatch.Testing;

using var q = QueryWatchScope.Start(exportJsonPath: "artifacts/ef.json", sampleTop: 200);

var options = new DbContextOptionsBuilder<MyDbContext>()
    .UseSqlite("Data Source=:memory:")
    .UseQueryWatch(q.Session)    // adds the interceptor
    .Options;
```
> Interceptor only records **executed** commands. Use `QueryWatchOptions` on the session to tune capture (text, parameter shapes, etc.).

---

## Dapper wiring

```csharp
using Dapper;
using Microsoft.Data.Sqlite;
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Testing;

using var q = QueryWatchScope.Start(exportJsonPath: "artifacts/dapper.json", sampleTop: 200);

await using var raw = new SqliteConnection("Data Source=:memory:");
await raw.OpenAsync();

// Wrap the provider connection so Dapper commands are recorded
using var conn = raw.WithQueryWatch(q.Session);

var rows = await conn.QueryAsync("SELECT 1");
```

> The extension returns the **ADO wrapper** when possible for high‑fidelity recording; otherwise it falls back to a Dapper‑specific wrapper.

---

## ADO.NET wiring

```csharp
using Microsoft.Data.Sqlite;
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Testing;

using var q = QueryWatchScope.Start(exportJsonPath: "artifacts/ado.json", sampleTop: 200);
await using var raw = new SqliteConnection("Data Source=:memory:");
await raw.OpenAsync();

using var conn = new QueryWatchConnection(raw, q.Session);
using var cmd  = conn.CreateCommand();
cmd.CommandText = "SELECT 1";
await cmd.ExecuteNonQueryAsync();
```

---

## Budgets (counts & timing)

At **test time** (scope) you can enforce `maxQueries`, `maxAverage`, `maxTotal`. At **CI time** use the CLI for stronger rules including **per‑pattern budgets**. Patterns support wildcards (`*`, `?`) or `regex:` prefix.

Example per‑pattern budgets:

```bash
--budget "SELECT * FROM Users*=1"
--budget "regex:^UPDATE Orders SET=3"
```

> If your JSON is **top‑N sampled**, budgets evaluate only over those events. Increase `sampleTop` in your export to tighten guarantees.

---

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

### Multi‑file support
Repeat `--input` to aggregate multiple JSONs (e.g., per‑project reports in a mono‑repo). Budgets evaluate on the aggregate.

### GitHub PR summary
When run inside GitHub Actions, the CLI writes a Markdown table to the **Step Summary** automatically.

---

## Troubleshooting

- **“Budget violations:” but no pattern table** → you didn’t pass any `--budget`, or your JSON was **heavily sampled**. Re‑export with higher `sampleTop` (e.g., 200–500).  
- **Baselines seem too strict** → tolerances are **percent of baseline**. Ensure your baseline is representative; use `--baseline-allow-percent` to allow small drift.  
- **CLI help in README looks stale** → run `./build/Update-ReadmeFlags.ps1` (or `--print-flags-md`) to refresh the block between markers.  
- **Hot path text capture** → disable per‑adapter: `QueryWatchOptions.Disable{Ado|Dapper|EfCore}TextCapture=true`.  
- **Parameter metadata** → **ON by default**. Set `QueryWatchOptions.CaptureParameterShape=false` (emits `event.meta.parameters`), never values.

---

## License
MIT
