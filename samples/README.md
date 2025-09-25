# QueryWatch Samples

This folder contains small **sample projects** that consume the `KeelMatrix.QueryWatch` NuGet package
from your local build. They are intentionally minimal so you can understand and test the core behaviors
quickly in different situations (EF Core + SQLite, and raw ADO).

> **Important:** These samples expect you to build the package(s) locally first and then add them to each sample.
> See the "Quick Start" below.

## Layout
- `EFCore.Sqlite/` – EF Core (SQLite provider) showing interceptor wiring and basic budgets.
- `Ado.Sqlite/` – plain ADO.NET over `Microsoft.Data.Sqlite` wrapped by `QueryWatchConnection`.
- `cli-examples.ps1` / `cli-examples.sh` – example commands for running the QueryWatch CLI gate.
- `NuGet.config` – forces the `KeelMatrix.QueryWatch*` packages to come from your local `../artifacts/packages`.
- `.gitignore` – ignores local build outputs and DB files for samples only.

## Quick Start (local, step-by-step)
1. **Pack the libraries** at the repository root (one level *above* this folder):
   ```bash
   dotnet pack ./src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj -c Release --include-symbols --p:SymbolPackageFormat=snupkg --output ./artifacts/packages
   dotnet pack ./src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj -c Release --include-symbols --p:SymbolPackageFormat=snupkg --output ./artifacts/packages
   ```
2. **Install the packages into each sample** (runs the add+restore for you):
   - Windows (PowerShell): `./init.ps1`
   - Linux/macOS (bash): `./init.sh`
   These scripts run:
   ```bash
   dotnet add ./EFCore.Sqlite/EFCore.Sqlite.csproj package KeelMatrix.QueryWatch
   dotnet add ./EFCore.Sqlite/EFCore.Sqlite.csproj package KeelMatrix.QueryWatch.EfCore
   dotnet add ./Ado.Sqlite/Ado.Sqlite.csproj package KeelMatrix.QueryWatch
   ```
   The included `NuGet.config` pins `KeelMatrix.QueryWatch*` to the local `../artifacts/packages` folder.
3. **Run a sample** (EF Core example shown):
   ```bash
   dotnet run --project ./EFCore.Sqlite/EFCore.Sqlite.csproj
   ```
   You should see console output and a file at `./EFCore.Sqlite/bin/Debug/net8.0/artifacts/qwatch.ef.json`.
4. **Gate with the CLI** (from repo root or here):
   ```bash
   dotnet run --project ../tools/KeelMatrix.QueryWatch.Cli -- --input ./EFCore.Sqlite/bin/Debug/net8.0/artifacts/qwatch.ef.json --max-queries 50
   ```

### Notes
- These samples **compile only after** you add the `KeelMatrix.QueryWatch` and (for EF) `KeelMatrix.QueryWatch.EfCore` packages (Step 2).
- If you get restore errors, confirm that `../artifacts/packages` exists and contains your `*.nupkg` files.
- The EF Core sample uses a file-based SQLite DB under `./EFCore.Sqlite/app.db`. You can delete it safely.
