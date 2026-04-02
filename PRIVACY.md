# Privacy

QueryWatch uses `KeelMatrix.Telemetry` transitively for minimal anonymous usage telemetry.

This repository is not the source of truth for telemetry implementation details such as:
- event types
- emitted fields
- opt-out environment variables
- local storage layout
- network endpoint
- retention behavior

Those details can change with the telemetry package and are maintained in the telemetry repository:

- Telemetry README: https://github.com/KeelMatrix/Telemetry#readme
- Telemetry privacy policy: https://github.com/KeelMatrix/Telemetry/blob/main/PRIVACY.md

## QueryWatch-specific note

QueryWatch packages use the shared telemetry package, but not every package uses every shared event type.

- The main `KeelMatrix.QueryWatch` library uses activation and heartbeat through its session lifecycle.
- The `qwatch` CLI sends an activation event on normal execution, but does not send heartbeat events.
- `qwatch telemetry status`, `qwatch telemetry disable`, and `qwatch telemetry enable` do not emit telemetry.
- `qwatch telemetry disable` writes a repo-local opt-out only when it can safely create or update a qwatch-managed config.
- `qwatch telemetry enable` only removes or neutralizes qwatch-managed repo-local opt-out state.
- QueryWatch-owned repo-local configs use `managedBy: "qwatch"` as the ownership marker.
- Higher-precedence process environment variables still override repo-local config.

QueryWatch does not add product-specific telemetry fields on top of the shared telemetry package behavior documented above. If that changes in a way that affects privacy, this file will be updated.
