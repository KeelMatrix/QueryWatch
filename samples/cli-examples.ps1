Param()
$ErrorActionPreference = 'Stop'
# Example CLI runs (adjust paths if you execute from a different folder)

# EF Core sample JSON
dotnet run --project ../tools/KeelMatrix.QueryWatch.Cli -- --input ./EFCore.Sqlite/bin/Debug/net8.0/artifacts/qwatch.ef.json --max-queries 50

# ADO sample JSON
dotnet run --project ../tools/KeelMatrix.QueryWatch.Cli -- --input ./Ado.Sqlite/bin/Debug/net8.0/artifacts/qwatch.ado.json --max-queries 50

# Baseline workflow (write, then compare with +10% tolerance)
dotnet run --project ../tools/KeelMatrix.QueryWatch.Cli -- --input ./EFCore.Sqlite/bin/Debug/net8.0/artifacts/qwatch.ef.json --baseline ./EFCore.Sqlite/bin/Debug/net8.0/artifacts/baseline.json --write-baseline
dotnet run --project ../tools/KeelMatrix.QueryWatch.Cli -- --input ./EFCore.Sqlite/bin/Debug/net8.0/artifacts/qwatch.ef.json --baseline ./EFCore.Sqlite/bin/Debug/net8.0/artifacts/baseline.json --baseline-allow-percent 10
