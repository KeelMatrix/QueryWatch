# KeelMatrix.QueryWatch.Redaction

Redaction helpers for QueryWatch query text.

Use this package to normalize or mask sensitive SQL text before reports are asserted, exported, or shared in CI.

## Install

```bash
dotnet add package KeelMatrix.QueryWatch
dotnet add package KeelMatrix.QueryWatch.Redaction
```

## What It Helps With

- remove bearer tokens, API keys, cookies, and connection-string secrets
- hide email addresses, GUID-like identifiers, and other noisy values
- normalize whitespace to make diffs and snapshots easier to review
- keep test diagnostics useful without leaking sensitive values

## Quick Example

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Redaction;

var options = new QueryWatchOptions()
    .UseRecommendedRedactors(
        includeTimestamps: false,
        includeIpAddresses: false,
        includePhone: false);
```

## Common Use Cases

- redact query text before exporting JSON artifacts from CI
- make snapshots stable by removing rotating IDs or timestamps
- sanitize SQL diagnostics before attaching them to pull requests or support issues

## Notes

- Targets `net8.0` and `netstandard2.0`
- Zero external runtime dependencies
- Regex handling is hardened with short timeouts and, where supported, non-backtracking mode

## Documentation

- [Repository](https://github.com/KeelMatrix/QueryWatch)
- [Root README](https://github.com/KeelMatrix/QueryWatch#readme)
