# qwatch

`qwatch` is the QueryWatch command-line tool for evaluating QueryWatch JSON summaries in CI and local verification workflows.

Use it to:
- fail builds when query counts exceed budget
- gate pull requests on average or total SQL duration
- compare current results against a checked-in baseline
- enforce per-pattern query budgets
- aggregate multiple summary files from a mono-repo or multi-test pipeline

## Install

If packed as a dotnet tool:

```bash
dotnet tool install --global qwatch
```

Or run it from source in this repo:

```bash
dotnet run --project ./tools/KeelMatrix.QueryWatch.Cli -- --help
```

## Basic Usage

Show help:

```bash
qwatch --help
```

Inspect effective telemetry state and repo-local config status for the current repo:

```bash
qwatch telemetry status
```

Write a qwatch-managed repo-local telemetry opt-out for the current repo:

```bash
qwatch telemetry disable
```

Remove a qwatch-managed repo-local telemetry opt-out for the current repo:

```bash
qwatch telemetry enable
```

Fail if total queries exceed 50:

```bash
qwatch --input ./artifacts/qwatch.json --max-queries 50
```

Fail if average SQL duration exceeds 20 ms:

```bash
qwatch --input ./artifacts/qwatch.json --max-average-ms 20
```

Fail if total SQL time exceeds 100 ms:

```bash
qwatch --input ./artifacts/qwatch.json --max-total-ms 100
```

## Baseline Comparisons

Compare current results to a baseline and allow a small regression window:

```bash
qwatch \
  --input ./artifacts/current.json \
  --baseline ./artifacts/baseline.json \
  --baseline-allow-percent 10
```

Write or refresh a baseline:

```bash
qwatch \
  --input ./artifacts/current.json \
  --baseline ./artifacts/baseline.json \
  --write-baseline
```

This is useful when you want a stable performance reference checked into the repo and reviewed over time.

## Pattern Budgets

Pattern budgets let you limit how many times specific SQL shapes may appear.

Wildcard example:

```bash
qwatch --input ./artifacts/qwatch.json --budget "SELECT * FROM Users*=1"
```

Regex example:

```bash
qwatch --input ./artifacts/qwatch.json --budget "regex:^UPDATE Orders SET=3"
```

Use this for:
- blocking N+1 query families
- capping repeated lookup queries
- protecting hot-path commands from accidental fan-out

## Multiple Input Files

Repeat `--input` to aggregate results from multiple projects or test runs:

```bash
qwatch \
  --input ./artifacts/api-tests.json \
  --input ./artifacts/integration-tests.json \
  --max-queries 120
```

This is especially useful in mono-repos or when each test project emits its own QueryWatch summary.

## Full Event Requirements

If your summaries are top-N sampled, budgets only evaluate over the captured events. To ensure the CLI only accepts full event sets:

```bash
qwatch --input ./artifacts/qwatch.json --require-full-events
```

Use this in stricter CI gates where partial event capture is not acceptable.

## CI Usage

Typical flow:

1. Run tests and export QueryWatch JSON summaries.
2. Invoke `qwatch` with budgets or baseline comparison.
3. Fail the job if the exit code is non-zero.

In GitHub Actions, the CLI also writes a Markdown summary to the step summary when applicable.

## Input Expectations

`qwatch` expects QueryWatch summary JSON produced by the core library, for example via `QueryWatchJson.ExportToFile(...)`.

If you only need the file format in another tool, see the contracts package:

- `KeelMatrix.QueryWatch.Contracts`

## Privacy

`qwatch` sends a minimal anonymous telemetry activation event on normal CLI execution.

Telemetry management commands do not emit telemetry. Use `qwatch telemetry status`, `qwatch telemetry disable`, and `qwatch telemetry enable` to inspect or manage the repo-local opt-out file without introducing a second config model. These commands stay repo-scoped to the current working directory, and process environment variables still take precedence over repo-local config. QueryWatch-owned files use `managedBy: "qwatch"` as the ownership marker. If an existing `keelmatrix.telemetry.json` is not qwatch-managed, the CLI fails safely instead of overwriting it.

It does not send heartbeat events. Reason: `qwatch` is typically a short-lived CI/local tool, so weekly heartbeat would mostly reflect retained pipeline wiring rather than meaningful interactive product usage.

See:
- [Repository privacy summary](../../PRIVACY.md)
- [KeelMatrix.Telemetry README](https://github.com/KeelMatrix/Telemetry#readme)

## Related Documentation

- [Root README](https://github.com/KeelMatrix/QueryWatch#readme)
- [Core package](https://github.com/KeelMatrix/QueryWatch/tree/main/src/KeelMatrix.QueryWatch)
- [Troubleshooting](https://github.com/KeelMatrix/QueryWatch#troubleshooting)
