# QueryWatch Samples

Tiny apps that consume the local `KeelMatrix.QueryWatch*` packages so you can see EF Core, ADO.NET, and Dapper wiring in action.

## Layout
- `EFCore.Sqlite/` – EF Core (SQLite) with interceptor wiring and basic budgets.
- `Ado.Sqlite/` – plain ADO.NET over `Microsoft.Data.Sqlite` via `QueryWatchConnection`.
- `Dapper.Sqlite/` – Dapper (async + transactions) via `WithQueryWatch(...)`.
- `cli-examples.ps1` / `cli-examples.sh` – quick commands to run the CLI gate.
- `NuGet.config` – pins `KeelMatrix.QueryWatch*` to `../artifacts/packages` (local build).

## Start here
Follow the **[Quick Start — Samples (local)](../README.md#quick-start--samples-local)** in the root README.

> After you run `dotnet pack ...` at the repo root, use `./init.ps1` (or `./init.sh`) once to add local packages. No other tweaks are needed.

### Run a sample
```bash
dotnet run --project ./EFCore.Sqlite/EFCore.Sqlite.csproj -c Release
```

For CLI usage examples, see `cli-examples.ps1` / `cli-examples.sh`.
