# 🧰 Build Scripts

Helper scripts for local development and CI.  
Run from the **repo root** unless otherwise noted.

---

## 📦 What’s Here

- **`Dev-PackInstallSamples.ps1` / `.sh`** — Restore from NuGet.org, build, and **pack** the local `KeelMatrix.QueryWatch` and `KeelMatrix.QueryWatch.EfCore` packages, then restore **samples** against the locally packed feed (`./artifacts/packages`).
  → `build/Dev-PackInstallSamples.ps1` • `build/Dev-PackInstallSamples.sh`

- **`Dev-CleanPackInstallSamples.ps1` / `.sh`** — Same as above, but first **cleans** local QueryWatch packages and their local cache entries before rebuilding & packing. Ideal when iterating locally.
  → `build/Dev-CleanPackInstallSamples.ps1` • `build/Dev-CleanPackInstallSamples.sh`

The local feed is only for QueryWatch packages built from this repository. Redaction and Telemetry are always restored from NuGet.org.

- **`Update-ReadmeFlags.ps1`** — Builds the CLI and updates the README block between  
  `<!-- BEGIN:CLI_FLAGS -->` and `<!-- END:CLI_FLAGS -->` using `--print-flags-md`.  
  Writes fallback output to `docs/CLI_FLAGS.generated.md` if markers are missing.  
  → `build/Update-ReadmeFlags.ps1`

- **`Get-CodeScanningAlerts.ps1`** — Fetches GitHub code scanning alerts for a repository, categorizes them, and writes raw, categorized, and summary JSON outputs under `build/artifacts/security` by default.  
  → `build/Get-CodeScanningAlerts.ps1`

> PowerShell (`.ps1`) and Bash (`.sh`) variants are provided to support cross-platform workflows.

---

## ⚡ Quick Tasks

### Pack libs and restore samples (fast path)

#### Windows / PowerShell
```powershell
pwsh -NoProfile -File build/Dev-PackInstallSamples.ps1
```

#### Linux / macOS
```bash
bash build/Dev-PackInstallSamples.sh
```

The scripts restore/build/pack QueryWatch and then restore samples with `samples/NuGet.config`.

---

### Clean old local packages, repack, restore samples

```powershell
pwsh -NoProfile -File build/Dev-CleanPackInstallSamples.ps1
```

```bash
bash build/Dev-CleanPackInstallSamples.sh
```

---

### Refresh CLI flags in README

```powershell
./build/Update-ReadmeFlags.ps1
```

---

### Export code scanning alerts

```powershell
./build/Get-CodeScanningAlerts.ps1
```

Optional parameters:

- `-Repo <owner/name>`  
  Explicit GitHub repository to query. If omitted, the script tries `gh repo view`, then falls back to the `origin` git remote.

- `-State <open|dismissed|fixed|all>`  
  Alert state filter. Default: `open`.

- `-OutputRoot <path>`  
  Output directory for generated files. Default: `build/artifacts/security` under the repo root.

Examples:

```powershell
./build/Get-CodeScanningAlerts.ps1 -Repo KeelMatrix/QueryWatch
./build/Get-CodeScanningAlerts.ps1 -State all
./build/Get-CodeScanningAlerts.ps1 -OutputRoot .\build\artifacts\security
```

Outputs:

- `*.raw.json` — raw GitHub API alert payloads
- `*.categorized.json` — normalized alert records with category and triage metadata
- `*.summary.json` — aggregate counts, top files/rules, and recommended triage buckets

Prerequisites:

- GitHub CLI (`gh`) installed
- `gh auth login` completed
- `git` available

---

## 🧩 Prerequisites

- **.NET SDK 8.x+** → check via:
  ```bash
  dotnet --info
  ```
  See: `docs/DEV.md`

---

## 📁 Conventions

- Artifacts are written to `./artifacts` (subfolders: `packages/`, `benchmarks/`, etc).  
  See: `build/Dev-PackInstallSamples.ps1` or `bench/README.md`.

---
