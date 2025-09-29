# KeelMatrix.QueryWatch.Redaction

This library contains *implementations* of redactors and helper utilities for QueryWatch.

- Targets **net8.0** and **netstandard2.0**
- **Zero external runtime dependencies**
- Hardened regex: `RegexOptions.NonBacktracking` (on .NET 8+) and short match timeouts

## Usage

```csharp
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Redaction;

var options = new QueryWatchOptions()
    .UseRecommendedRedactors(includeTimestamps: false, includeIpAddresses: false, includePhone: false);
```

> Noisy redactors — timestamps, IPs, phone numbers — are **optional** and disabled by default to avoid false positives.
