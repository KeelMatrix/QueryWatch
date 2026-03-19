# KeelMatrix.QueryWatch.EfCore

EF Core integration package for QueryWatch.

It adds:
- `UseQueryWatch(...)` for `DbContextOptionsBuilder`
- a command interceptor that records executed EF Core commands into a `QueryWatchSession`

## Install

```bash
dotnet add package KeelMatrix.QueryWatch
dotnet add package KeelMatrix.QueryWatch.EfCore
```

## Quick Example

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.EfCore;

using var session = new QueryWatchSession();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("Data Source=:memory:")
    .UseQueryWatch(session)
    .Options;
```

After running the workload:

```csharp
var report = session.Complete();
report.ShouldHaveExecutedAtMost(10);
```

## When To Use This Package

Use `KeelMatrix.QueryWatch.EfCore` when your data access goes through EF Core and you want to:
- catch N+1 regressions in application or integration tests
- enforce SQL budgets per test or per scenario
- export EF-generated SQL summaries into CI pipelines

## Documentation

- [EF Core wiring](https://github.com/KeelMatrix/QueryWatch#ef-core-wiring)
- [Root README](https://github.com/KeelMatrix/QueryWatch#readme)
- [Troubleshooting](https://github.com/KeelMatrix/QueryWatch#troubleshooting)
