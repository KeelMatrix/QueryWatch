# QueryWatch

**Status:** core ready to use in tests. Adapters: EF Core and ADO/Dapper wrappers. JSON export for CI included.

## Quickstart (per‑test scope + JSON)

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Testing;

// Fail if more than 5 queries OR avg > 50ms; also export JSON for CI gate.
using var q = QueryWatch.Testing.QueryWatchScope.Start(
    maxQueries: 5,
    maxAverage: TimeSpan.FromMilliseconds(50),
    exportJsonPath: "artifacts/qwatch.report.json",
    sampleTop: 50); // increase if you rely on CLI per‑pattern budgets

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
QueryWatchJson.ExportToFile(report, "artifacts/qwatch.report.json", sampleTop: 50);
```

The JSON includes a `meta.sampleTop` value to document how many events were sampled.

## CLI gate

Run after tests (ci.yml already contains a guarded step):

```pwsh
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --max-queries 50
# Allow +10% vs baseline and enforce a per‑pattern budget:
dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --baseline artifacts/qwatch.base.json --baseline-allow-percent 10 --budget "SELECT * FROM Users*=1"
```

## Built‑in redactors (opt‑in)

To avoid leaking sensitive data in recorded SQL and to make pattern matching more stable across OS/providers, you can enable the built‑in redactors.

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Redaction;
using KeelMatrix.QueryWatch.Testing;

var opts = new QueryWatchOptions()
    .UseRecommendedRedactors(); // whitespace normalize + mask emails, long hex tokens, JWTs

using var scope = QueryWatchScope.Start(
    maxQueries: 5,
    maxAverage: TimeSpan.FromMilliseconds(50),
    options: opts,
    exportJsonPath: "artifacts/qwatch.report.json",
    sampleTop: 50);

// ...execute code under test...
```

> The recommended set runs a whitespace normalizer first, then masks emails, long hex strings and JWT‑like tokens with "***".
> You can add your own rules via `AddRegexRedactor(pattern)` and control ordering by pushing items into `QueryWatchOptions.Redactors`.

### Additional built‑in redactors (opt‑in)
These are off by default. Add them explicitly if your logs/SQL contain these artifacts:

```csharp
using KeelMatrix.QueryWatch.Redaction;

var opts = new QueryWatchOptions().UseRecommendedRedactors();
// Opt into specific extras:
opts.Redactors.Add(new ApiKeyRedactor());       // X-Api-Key / ApiKey headers & params
opts.Redactors.Add(new UuidNoDashRedactor());   // 32-hex UUIDs without dashes
opts.Redactors.Add(new CookieRedactor());       // Cookie / Set-Cookie headers
opts.Redactors.Add(new GoogleApiKeyRedactor()); // AIza... Google API keys
```

> You can control ordering by the sequence you add them (normalize first, then PII/secrets, then noise).
> Consider adding stronger shapes next (credit cards with Luhn, IBAN, Slack/GitHub tokens) if you see them in reports.
