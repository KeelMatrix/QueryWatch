# QueryWatch

**Status:** core ready to use in tests. Adapters: EF Core and ADO/Dapper wrappers. JSON export for CI included.

## Quickstart (per-test scope + JSON)

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Testing;

// Fail if more than 5 queries OR avg > 50ms; also export JSON for CI gate.
using var q = QueryWatch.Testing.QueryWatchScope.Start(
    maxQueries: 5,
    maxAverage: TimeSpan.FromMilliseconds(50),
    exportJsonPath: "artifacts/qwatch.report.json");

// Wire EF Core to q.Session (optional):
// var opts = new DbContextOptionsBuilder<MyDbContext>()
//     .UseInMemoryDatabase("test")
//     .UseQueryWatch(q.Session)
//     .Options;

// ... run code under test ...
// disposal writes JSON and enforces the budgets
```

## JSON API

```csharp
using KeelMatrix.QueryWatch.Reporting;

var report = session.Stop();
QueryWatchJson.ExportToFile(report, "artifacts/qwatch.report.json", sampleTop: 5);
```

## CLI gate

Run after tests (ci.yml already contains a guarded step):

```pwsh
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --max-queries 50
```
