# QueryWatch (skeleton)

**Status:** high-level skeleton, ready to be wired in tests. Low-level internals (providers, advanced redaction, CI, SaaS) are TODO.

## Quickstart (manual)

```csharp
using KeelMatrix.QueryWatch;

using var session = QueryWatcher.Start(new QueryWatchOptions { MaxQueries = 5 });
// Manually record (for plain ADO.NET scenarios):
session.Record("SELECT 1", TimeSpan.FromMilliseconds(2));

var report = session.Stop()
    .ShouldHaveExecutedAtMost(5)
    .ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(50));
```

## EF Core (net8.0)

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.EfCore;
using Microsoft.EntityFrameworkCore;

using var session = QueryWatcher.Start(new QueryWatchOptions { MaxQueries = 5 });

var opts = new DbContextOptionsBuilder<MyDbContext>()
    .UseInMemoryDatabase("test")
    .UseQueryWatch(session) // registers interceptor
    .Options;

using var db = new MyDbContext(opts);
// ... run code under test ...
session.Stop().ThrowIfViolations();
```

## Design (phase 1)

- **Core** (`KeelMatrix.QueryWatch`): provider-agnostic session + report + simple fluent checks.
- **Adapter (EF Core)** (`KeelMatrix.QueryWatch.EfCore`): `DbCommandInterceptor` that records into a session. Compiles only for `net8.0` via conditional TFMs.
- **TODOs:**
  - PII/secret redaction in SQL text (configurable rules).
  - Baseline regression comparisons and per-test scopes.
  - Dapper / ADO.NET wrappers for transparent interception.
  - Telemetry (opt-in) + CI companion.
  - Public API hardening (pre-1.0).

See `QueryWatchOptions` for thresholds and `QueryWatchReport` for checks.