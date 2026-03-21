# QueryWatch Samples

Tiny apps that consume the local `KeelMatrix.QueryWatch*` packages, plus the shared `KeelMatrix.Redaction` and `KeelMatrix.Telemetry` dependencies, so you can see EF Core, ADO.NET, and Dapper wiring in action.

## Layout
- `EFCore.Sqlite/` – EF Core (SQLite) with interceptor wiring and basic budgets.
- `Ado.Sqlite/` – plain ADO.NET over `Microsoft.Data.Sqlite` via `WithQueryWatch(...)`.
- `Dapper.Sqlite/` – Dapper (async + transactions) via `WithQueryWatch(...)`.
- `cli-examples.ps1` / `cli-examples.sh` – quick commands to run the CLI gate.
- `NuGet.config` – pins local `KeelMatrix.QueryWatch*`, `KeelMatrix.Redaction*`, and `KeelMatrix.Telemetry*` packages to `../artifacts/packages`.

## Start here
Follow the **[Quick Start — Samples (local)](../README.md#quick-start--samples-local)** in the root README.

> Keep sibling checkouts of `KeelMatrix.Redaction` and `KeelMatrix.Telemetry` next to this repo, then run `pwsh -NoProfile -File ../build/Dev-PackInstallSamples.ps1` (or `bash ../build/Dev-PackInstallSamples.sh`) once from the repo root to stage all local packages. No other tweaks are needed.

### Run a sample
```bash
dotnet run --project ./EFCore.Sqlite/EFCore.Sqlite.csproj -c Release
```

For CLI usage examples, see `cli-examples.ps1` / `cli-examples.sh`.
