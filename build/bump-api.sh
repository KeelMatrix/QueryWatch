#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Debug}"
SOLUTION="KeelMatrix.QueryWatch.sln"

echo "==> Restoring packages..."
dotnet restore "$SOLUTION"

echo "==> Building ($CONFIGURATION)..."
dotnet build "$SOLUTION" -c "$CONFIGURATION" --no-restore

echo "==> Applying Public API analyzer fixes (RS0016/RS0017)..."
dotnet format analyzers "$SOLUTION" --diagnostics RS0016,RS0017 --no-restore --verbosity minimal

echo "==> Formatting whitespace..."
dotnet format whitespace "$SOLUTION" --no-restore --verbosity quiet

echo "==> Validating build..."
dotnet build "$SOLUTION" -c "$CONFIGURATION" --no-restore

echo "==> Done. Review changes under *PublicAPI.Unshipped.txt* and commit them."
