# KeelMatrix.QueryWatch.Contracts

Dependency-light contract package for the QueryWatch JSON format.

This package is useful when you want to read, write, or transform QueryWatch summary files without depending on the full runtime package.

## Install

```bash
dotnet add package KeelMatrix.QueryWatch.Contracts
```

## What It Contains

- `Summary` for file-level query summary data
- `EventSample` for sampled query-event entries
- `QueryWatchJsonContext` for source-generated `System.Text.Json` serialization

## Typical Scenarios

- build a custom CI/reporting step around QueryWatch JSON
- inspect saved summaries in a separate tool or service
- keep contract handling lightweight in utilities that do not need query instrumentation

## Documentation

- [Repository](https://github.com/KeelMatrix/QueryWatch)
- [Root README](https://github.com/KeelMatrix/QueryWatch#readme)
