# 🧰 Build Scripts

Helper scripts for local development and CI.  
Run from the **repo root** unless otherwise noted.

---

## 📦 What’s Here

- **`Dev-PackInstallSamples.ps1` / `.sh`** — Restore, build, and **pack** `KeelMatrix.QueryWatch*` libraries, then restore **samples** against the locally packed feed (`./artifacts/packages`).  
  → `build/Dev-PackInstallSamples.ps1` • `build/Dev-PackInstallSamples.sh`

- **`Dev-CleanPackInstallSamples.ps1` / `.sh`** — Same as above, but first **cleans** local `KeelMatrix.QueryWatch*.nupkg` / `.snupkg` before rebuilding & packing. Ideal when iterating locally.  
  → `build/Dev-CleanPackInstallSamples.ps1` • `build/Dev-CleanPackInstallSamples.sh`

- **`Update-ReadmeFlags.ps1`** — Builds the CLI and updates the README block between  
  `<!-- BEGIN:CLI_FLAGS -->` and `<!-- END:CLI_FLAGS -->` using `--print-flags-md`.  
  Writes fallback output to `docs/CLI_FLAGS.generated.md` if markers are missing.  
  → `build/Update-ReadmeFlags.ps1`

- **`Pack-Sign-Push.ps1`** — End-to-end **pack → (optional) sign → push** workflow.  
  Stubs for signing/publishing (customize for your environment).  
  → `build/Pack-Sign-Push.ps1`

- **`New-DevSecrets.ps1`** — Stub that documents how to configure your **NuGet API key** and import a **code-signing certificate** locally. Safe to customize for your organization.  
  → `build/New-DevSecrets.ps1`

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

### End-to-end pack → (optional) sign → push

Customize first, then run:
```powershell
./build/Pack-Sign-Push.ps1
```

---

## 🧩 Prerequisites

- **.NET SDK 8.x+** → check via:
  ```bash
  dotnet --info
  ```
  See: `docs/DEV.md`
- For signing/publish flows: configure your **NuGet API key** and (optional) **code-signing certificate** locally.  
  See: `build/New-DevSecrets.ps1`

---

## 📁 Conventions

- Artifacts are written to `./artifacts` (subfolders: `packages/`, `benchmarks/`, etc).  
  See: `build/Dev-PackInstallSamples.ps1` or `bench/README.md`.

---
