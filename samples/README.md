# QueryWatch Samples

Tiny apps that consume the local `KeelMatrix.QueryWatch*` packages, plus the shared `KeelMatrix.Redaction` and `KeelMatrix.Telemetry` dependencies, so you can see EF Core, ADO.NET, and Dapper wiring in action.

## Layout
- `EFCore.Sqlite/` – EF Core (SQLite) with interceptor wiring and basic budgets.
- `Ado.Sqlite/` – plain ADO.NET over `Microsoft.Data.Sqlite` via `WithQueryWatch(...)`.
- `Dapper.Sqlite/` – Dapper (async + transactions) via `WithQueryWatch(...)`.
- `cli-examples.ps1` / `cli-examples.sh` – quick commands to run the CLI gate.
- `NuGet.config` – pins local `KeelMatrix.QueryWatch*` packages to `../artifacts/packages`; Redaction and Telemetry come from NuGet.org.

## Start here
Follow the **[Quick Start — Samples (local)](../README.md#quick-start--samples-local)** in the root README.

The helper scripts pack QueryWatch packages locally and restore Redaction and Telemetry 0.1.0 from NuGet.org. Run `pwsh -NoProfile -File ../build/Dev-PackInstallSamples.ps1` (or `bash ../build/Dev-PackInstallSamples.sh`) once from the repo root.

### Run a sample
```bash
dotnet run --project ./EFCore.Sqlite/EFCore.Sqlite.csproj -c Release
```

For CLI usage examples, see `cli-examples.ps1` / `cli-examples.sh`.
