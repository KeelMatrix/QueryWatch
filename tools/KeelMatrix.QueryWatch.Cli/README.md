# qwatch (QueryWatch CLI)

QueryWatch CLI lets you **analyze captured SQL** and **enforce query budgets** in CI/CD.

## Usage examples

### Show help
```bash
qwatch --help
```

### Fail if total queries exceed 50
```bash
qwatch --input ./artifacts/qwatch.json --max-queries 50
```

### Compare against a baseline with tolerance
```bash
qwatch --input ./artifacts/qwatch.json --baseline ./artifacts/baseline.json --baseline-allow-percent 10
```

## See also
- [Quick Start — Samples (local)](https://github.com/KeelMatrix/QueryWatch#quick-start--samples-local)
- [Troubleshooting](https://github.com/KeelMatrix/QueryWatch#troubleshooting)
