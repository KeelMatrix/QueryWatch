# DEV.md — QueryWatch Developer Guide

> This doc is a **hands-on cheat sheet** for day-to-day work on QueryWatch. Every block is copy‑pasteable.
> Comments explain the *why*, not just the *what* — so new contributors don’t have to reverse‑engineer the repo.

---

## 0) Prereqs

- .NET SDK 8.x or newer installed (`dotnet --info` should show a compatible SDK).
- Windows PowerShell (`pwsh`) recommended, but Bash works too (Linux/macOS).
- Git (for SourceLink & versioning).
- Local QueryWatch development expects sibling checkouts of `../KeelMatrix.Redaction/app` and `../KeelMatrix.Telemetry/app` so their packages can be staged into `./artifacts/packages`.
- For EF Core tests: nothing extra — they use SQLite in-memory.

```bash
dotnet --info
```

---

## 1) Build & Test (fast iteration)

```bash
# stage local dependency packages first
pwsh -NoProfile -File build/Dev-PackInstallSamples.ps1

# restore once
dotnet restore KeelMatrix.QueryWatch.sln

# build everything
dotnet build KeelMatrix.QueryWatch.sln -c Debug

# run tests (unit + integration)
dotnet test KeelMatrix.QueryWatch.sln -c Debug
```

**Why**: keep Debug quick. Release builds promote warnings to errors in packable projects and do trimming for the CLI (see below).

---

## 2) CLI — Single‑file publish (small, fast startup)

> We ship a **framework‑dependent single file** by default to avoid RID explosion. It trims safely because the CLI consumes source‑generated JSON models.

```bash
# publish QueryWatch.Cli as a single file (Release)
dotnet publish tools/KeelMatrix.QueryWatch.Cli/KeelMatrix.QueryWatch.Cli.csproj \
  -c Release -p:PublishProfile=SingleFile -o ./artifacts/cli-singlefile
```

- Output: small apphost + embedded assemblies under `./artifacts/cli-singlefile`.  
- Toggle trimming off if you suspect new reflection usage:
  ```bash
  dotnet publish tools/KeelMatrix.QueryWatch.Cli/KeelMatrix.QueryWatch.Cli.csproj \
    -c Release -p:PublishProfile=SingleFile -p:EnableCliTrim=false -o ./artifacts/cli-singlefile
  ```

---

## 3) Optional: Pack the CLI as a dotnet tool (deterministic versioning)

> Handy for devs/CI. We **require** a version when packing as a tool to keep artifacts deterministic.

```bash
# pack as tool with explicit version (deterministic)
dotnet pack tools/KeelMatrix.QueryWatch.Cli/KeelMatrix.QueryWatch.Cli.csproj \
  -c Release -p:PackAsDotNetTool=true -p:ToolVersion=0.1.0 -o ./artifacts/packages

# local install (global)
dotnet tool install -g qwatch --add-source ./artifacts/packages

# verify
qwatch --help
```

- Bump the `ToolVersion` on every public change of UX/flags. CI/CD can publish the `.nupkg` if you like.

---

## 4) “Single Source of Truth” for flags → keep README & --help in sync

> The CLI generates help **and** a Markdown snippet from the same internal spec.  
> A helper script updates README between markers (or writes a generated file if markers aren’t present).

**When to run** (short answer): *any time you add/change/remove a CLI flag or its description*. See the next section for a precise checklist.

```powershell
# Windows/PowerShell
./build/Update-ReadmeFlags.ps1
```

```bash
# Bash: same effect, call the CLI switch directly and paste output
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --print-flags-md
```

**Add these markers once** in `README.md` so the script can replace the block automatically:

```markdown
<!-- BEGIN:CLI_FLAGS -->
<!-- END:CLI_FLAGS -->
```

If markers are missing, the script writes `docs/CLI_FLAGS.generated.md` for manual copy/paste.

---

## 5) Exactly when should I run `Update-ReadmeFlags.ps1`? (checklist)

Run it whenever any of the following changes happen:

- You **add a new CLI option**, change a flag name, or change its **value syntax** (e.g., `--max-queries N` → `--max-queries <int>`).
- You **update flag descriptions** or notes (repeatability, regex usage, etc.).
- You change **help formatting rules** (column widths, headings).
- Before **cutting a release** or **opening a large PR** that touches CLI UX.
- When a reviewer says “help and README don’t match” 🙂

> Pro tip: wire it into a local `pre-commit` hook or a small CI check that diff‑compares the README block to `--print-flags-md` output.

---

## 6) Contracts & schema discipline (why trimming is safe)

- JSON contracts live in `KeelMatrix.QueryWatch.Contracts` with **System.Text.Json source‑generation**.
- Writers (library) emit schema **1.0.0** and readers (CLI) deserialize via the **generated context**, so reflection isn’t needed.
- When changing the schema, follow `docs/SCHEMA_CHANGE_CHECKLIST.md`.

Sanity: run contract/unit tests after changes:

```bash
dotnet test tests/KeelMatrix.QueryWatch.Tests/KeelMatrix.QueryWatch.Tests.csproj -c Debug
```

---

## 7) CLI usage examples (local)

```bash
# Gate a single JSON summary with hard budgets
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- \
  --input artifacts/qwatch.report.json \
  --max-queries 50 --max-average-ms 25 --max-total-ms 1500

# Baseline tolerance (+10% allowed)
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- \
  --input artifacts/qwatch.report.json \
  --baseline artifacts/qwatch.base.json \
  --baseline-allow-percent 10

# Pattern budgets (wildcards or regex)
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- \
  --input artifacts/qwatch.report.json \
  --budget "SELECT * FROM Users*=1" \
  --budget "regex:^UPDATE Orders SET=3"
```

> In GitHub Actions, the CLI auto‑appends a Markdown summary to the job **Step Summary** if `GITHUB_STEP_SUMMARY` is set.

---

## 8) Benchmarks (perf guardrails for redactors)

- Script: `bench/Run-Benchmarks.ps1` (CI‑aware; exports CSV/JSON/MD/HTML into `artifacts/benchmarks/<ts>/`).

```powershell
# Quick local perf check
pwsh -NoProfile -File bench/Run-Benchmarks.ps1 -Job Short -MaxStdevPct 12 -Ci -CoolDownSec 0
```

Read more in `bench/BENCHMARKS.md`.

---

## 9) CI nuggets you can reuse locally

- **Formatting check** (same as CI):
  ```bash
  dotnet format --verify-no-changes
  ```

- **Vuln scan** (package advisories):
  ```bash
  dotnet list KeelMatrix.QueryWatch.sln package --vulnerable
  ```

- **Release packing** (mirrors CI steps for packable projects):
  ```bash
  dotnet pack ./src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj -c Release --no-build --include-symbols --p:SymbolPackageFormat=snupkg -o ./artifacts/packages
  dotnet pack ./src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj -c Release --no-build --include-symbols --p:SymbolPackageFormat=snupkg -o ./artifacts/packages
  ```

- **Samples** (consume your locally packed packages):
  ```bash
  # From repo root, with sibling checkouts at ../KeelMatrix.Redaction/app
  # and ../KeelMatrix.Telemetry/app
  pwsh -NoProfile -File build/Dev-PackInstallSamples.ps1
  dotnet run --project samples/EFCore.Sqlite/EFCore.Sqlite.csproj -c Release
  ```

---

## 10) Conventions & gotchas

- **README flags**: keep the markers in place; re-run the updater after any CLI change.
- **Trimming**: if a future change adds heavy reflection to the CLI, publish with `-p:EnableCliTrim=false` temporarily.
- **Tests expecting strings**: several integration tests assert exact substrings like `Baseline written:` or `Baseline regressions:`; keep them stable.
- **Text capture**: use `QueryWatchOptions.CaptureSqlText=false` if a hot path should not record SQL text.
- **Parameter shapes**: **ON by default**. Set `QueryWatchOptions.CaptureParameterShape=false` to disable; exported JSON will include `event.meta.parameters`.

---

## 11) Common tasks (copy‑paste)

**Update README flags**  
```powershell
./build/Update-ReadmeFlags.ps1
```

**Pack & install CLI tool**  
```powershell
dotnet pack tools/KeelMatrix.QueryWatch.Cli/KeelMatrix.QueryWatch.Cli.csproj -c Release -p:PackAsDotNetTool=true -p:ToolVersion=0.1.0 -o ./artifacts/packages
dotnet tool install -g qwatch --add-source ./artifacts/packages
```

**Publish trimmed single‑file**  
```powershell
dotnet publish tools/KeelMatrix.QueryWatch.Cli/KeelMatrix.QueryWatch.Cli.csproj -c Release -p:PublishProfile=SingleFile -o ./artifacts/cli-singlefile
```

**Run gate with budgets**  
```powershell
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --max-queries 50 --max-average-ms 25 --max-total-ms 1500
```

**Write then compare to baseline**  
```powershell
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --baseline artifacts/qwatch.base.json --write-baseline
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --baseline artifacts/qwatch.base.json --baseline-allow-percent 10
```

---

### Appendix A — Troubleshooting quickies

- **`Budget violations:` but no pattern table?** Ensure you passed at least one `--budget` and that your JSON `events` aren’t overly sampled (increase `sampleTop` where you export).
- **`Baseline regressions:` printed for tiny drifts** — remember tolerances are **percent of baseline**; check if your baseline is representative.
- **CLI help looks stale** — run `--print-flags-md` and update the README block (or run the PowerShell helper).

---

Happy hacking 🚀
