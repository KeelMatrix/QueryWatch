# KeelMatrix.QueryWatch.Contracts

Small, dependency-light package holding the JSON contract for QueryWatch:
- `Summary` — compact file-level summary
- `EventSample` — per-event sample

It also ships a System.Text.Json source-generation context `QueryWatchJsonContext`
for AOT/size/perf and compile-time schema lock-in.
