#!/usr/bin/env bash
set -euo pipefail

step() { printf "\n==> %s\n" "$1"; }
run()  { printf "   %s\n" "$*" >&2; "$@"; }

SCRIPT_DIR="$( cd -- "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$REPO_ROOT"

step ".NET SDK info"
run dotnet --info >/dev/null

ARTIFACTS="$REPO_ROOT/artifacts"
PKG_DIR="$ARTIFACTS/packages"
mkdir -p "$PKG_DIR"

step "Restore QueryWatch from NuGet.org"
run dotnet restore "KeelMatrix.QueryWatch.sln" --configfile "NuGet.config"

step "Build QueryWatch libraries (Release)"
run dotnet build "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" -c Release --no-restore
run dotnet build "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" -c Release --no-restore

step "Pack QueryWatch libraries -> ./artifacts/packages"
COMMON_PACK_ARGS=(
  '--configuration' 'Release'
  '--no-build'
  '--include-symbols'
  '--p:SymbolPackageFormat=snupkg'
  '--p:Version=0.1.0'
  '--output' "$PKG_DIR"
)
run dotnet pack "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" "${COMMON_PACK_ARGS[@]}"
run dotnet pack "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" "${COMMON_PACK_ARGS[@]}"

step "Restore samples with their local QueryWatch package feed"
run dotnet restore "samples/QueryWatch.Samples.sln" --configfile "samples/NuGet.config"

step "Done"
echo "Packages are in: $PKG_DIR"
echo "Samples restored with QueryWatch packages from the local feed and Redaction/Telemetry from NuGet.org."
