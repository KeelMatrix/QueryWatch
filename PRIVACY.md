# Privacy Policy

QueryWatch includes a minimal, privacy-preserving telemetry system to understand high-level usage. Telemetry is **opt-out by default** and can be fully disabled by setting:

KEELMATRIX_NO_TELEMETRY=1

(or `true` / `yes`) in the environment before running your application.

---

## What is sent

QueryWatch emits at most two types of events:

### 1) Activation (once per project)
Sent only the first time a project uses QueryWatch.

### 2) Heartbeat (at most once per ISO week)
A lightweight signal that the project is still in use.

---

## Data fields included

All telemetry payloads contain only the following fields:

- event — "activation" or "heartbeat"  
- tool — always "querywatch"  
- toolVersion — library version (e.g. 1.0.0)  
- schemaVersion — currently 1  
- projectHash — stable, anonymous hash derived from the local project (not reversible)  
- runtime — short framework identifier (e.g. .NET 8.0)  
- os — windows, linux, osx, or unknown  
- ci — boolean indicating whether common CI environment variables are present  
- timestamp — UTC timestamp (activation only)  
- week — ISO week string (heartbeat only, e.g. 2025-W03)  

No SQL, no file paths, no machine names, no usernames, no IP addresses, and no user content are ever collected.

---

## Endpoint

Telemetry is sent via HTTPS to:

https://keelmatrix-nuget-telemetry.dz-bb6.workers.dev

Payloads are capped at 512 bytes and failures are always swallowed; telemetry can never affect application behavior.

---

## Disabling telemetry

Telemetry is disabled when any of the following environment variable values are set:

KEELMATRIX_NO_TELEMETRY=1  
KEELMATRIX_NO_TELEMETRY=true  
KEELMATRIX_NO_TELEMETRY=yes  

---

## Guarantees

- Telemetry is best-effort and never throws  
- Telemetry cannot block execution  
- Telemetry contains no personal data  
- Telemetry can be fully disabled  
