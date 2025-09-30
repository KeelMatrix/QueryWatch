# Benchmarks

This folder contains all **performance benchmarks** for the QueryWatch ecosystem.  
We use [BenchmarkDotNet](https://benchmarkdotnet.org/) to validate perf regressions in our redactors and related components.

---

## How to Run

Run the PowerShell script from the repo root:

```powershell
pwsh -NoProfile -File bench/Run-Benchmarks.ps1
```

This will:

- Discover all `*.Benchmarks.csproj` under `bench/` (the same folder as this file).
- Build and run them in **Release** for `.NET 8.0` (override with `-Framework net9.0`, etc.).
- Export CSV / JSON / Markdown reports into `artifacts/benchmarks/<timestamp>/` at the repo root.

---

## Common Use Cases

- **Run everything (default job, net8.0):**

  ```powershell
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1
  ```

- **Run everything from the RedactionBench class:**

  ```powershell
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1 `
  -Filter "*RedactionBench.*" `
  -Job Default `
  -Framework net8.0 `
  -CoolDownSec 0
  ```

- **Run only PhoneRedactor benchmarks:**

  ```powershell
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1 -Filter *Phone*
  ```

- **Use a shorter job (for quick iteration):**

  ```powershell
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1 -Job Short
  ```

- **Check for noisy results (fail if StdDev > 8%):**

  ```powershell
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1 -MaxStdevPct 8
  ```

- **Generate CI summary (Markdown):**

  ```powershell
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1 -Ci -MaxStdevPct 12
  ```

---

## Artifacts

Each run creates a timestamped directory under:

```
artifacts/benchmarks/<timestamp>/<ProjectName>/
```

Contents include:

- `.csv` (machine-readable results)  
- `.json` (structured data for tooling)  
- `.md` (GitHub-friendly report)  
- `.html` (full interactive report)  

---

## Notes

- BenchmarkDotNet artifacts are also cached in `BenchmarkDotNet.Artifacts/`, but for reproducible results, prefer the `artifacts/benchmarks` exports.  
- If you add new benchmark projects in `bench/`, name them `*.Benchmarks.csproj` and the script will pick them up automatically.  
- The `bench/Run-Benchmarks.ps1` script is CI-ready. Hook it into GitHub Actions or your pipeline to catch regressions.
