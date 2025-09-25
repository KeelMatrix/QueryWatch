#!/usr/bin/env bash
set -euo pipefail
echo "Adding KeelMatrix.QueryWatch packages to samples using local NuGet source (../artifacts/packages)..."
dotnet --info >/dev/null

dotnet add ./EFCore.Sqlite/EFCore.Sqlite.csproj package KeelMatrix.QueryWatch
dotnet add ./EFCore.Sqlite/EFCore.Sqlite.csproj package KeelMatrix.QueryWatch.EfCore
dotnet add ./Ado.Sqlite/Ado.Sqlite.csproj package KeelMatrix.QueryWatch

echo "Restore completed. You can now run the samples."
