## Navigation

* Start bug work at `tests/<matching-project>`; then open paired code in `src/<matching-project>` or `tools/KeelMatrix.QueryWatch.Cli`. 
* Start feature work at `src/KeelMatrix.QueryWatch` for core, `src/KeelMatrix.QueryWatch.EfCore` for EF Core, `src/KeelMatrix.QueryWatch.Contracts` for JSON/contracts, `tools/KeelMatrix.QueryWatch.Cli` for CLI. 
* Search order: target test project -> target source project -> shared build files (`Directory.Build.*`, `Directory.Packages.props`) -> solution/workflows only if command or packaging behavior is unclear. 
* For public API changes, check only the affected project’s `PublicAPI.*.txt` and update via `build/bump-api.*`. 
* Ignore `.github/`, `bench/`, `build/`, `samples/`, `artifacts/`, `BenchmarkDotNet.Artifacts/`, docs/policy files unless task is CI, packaging, benchmarks, samples, or docs. 
* Ignore `tests/KeelMatrix.QueryWatch.Providers.SmokeTests` unless task explicitly involves provider smoke coverage or docker compose. 

## Commands

* Restore once: `dotnet restore KeelMatrix.QueryWatch.sln`
* Build narrow project first: `dotnet build <project.csproj> -c Release --no-restore`
* Test narrow project first: `dotnet test <test.csproj> -c Release --no-build --framework net8.0`
* Fast default validation: `dotnet test KeelMatrix.QueryWatch.sln -c Release --no-build --framework net8.0 --filter "FullyQualifiedName!~KeelMatrix.QueryWatch.Providers.SmokeTests"` 
* Smoke only when needed: `dotnet test tests/KeelMatrix.QueryWatch.Providers.SmokeTests/KeelMatrix.QueryWatch.Providers.SmokeTests.csproj -c Release --framework net8.0 --filter "FullyQualifiedName~KeelMatrix.QueryWatch.Providers.SmokeTests"` 
* Netstandard validation only when touching packable libraries/TFMs: `dotnet build src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj -c Release --no-restore -f netstandard2.0`
* CLI validation only when touching CLI: `dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --help`
* Format only before finalizing changed C# files: `dotnet format --verify-no-changes`

## Constraints

* Modify only files in the affected source/test project.
* Add or edit tests only in the matching test project.
* Do not touch solution, workflows, samples, benchmarks, or build scripts unless task requires it.
* Do not broad-refactor namespaces, folders, or shared build config.
* Keep API surface stable unless request requires API change.
* When changing public API, update only the affected `PublicAPI.*.txt`. 

## Efficiency Rules

* Do not scan entire repo.
* Read `KeelMatrix.QueryWatch.sln` first for project map; stop after locating target project. 
* Prefer `rg`/targeted reads by symbol, class, test name, or project name.
* Open one test file before multiple source files.
* Reuse the first relevant project/test pair; do not hop across projects without evidence.
* Do not reopen files already inspected unless edited.
* Do not spawn subagents.
* Do not rerun the same command after a non-environmental failure.
* Escalate validation from narrow test -> project build -> filtered solution test only if needed.

## Output

* Return minimal diffs only.
* No explanations unless requested.
* List only changed files and exact commands run.
* Do not include unrelated findings.
