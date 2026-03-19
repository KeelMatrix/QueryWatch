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

QueryWatch does not add product-specific telemetry fields on top of the shared telemetry package behavior documented above. If that changes in a way that affects privacy, this file will be updated.
