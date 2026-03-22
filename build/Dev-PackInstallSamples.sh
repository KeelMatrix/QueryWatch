#!/usr/bin/env bash
set -euo pipefail

step() { printf "\n==> %s\n" "$1"; }
run()  { printf "   %s\n" "$*" >&2; "$@"; }

SCRIPT_DIR="$( cd -- "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$REPO_ROOT"

# CI can override sibling checkout discovery with QW_*_REPO_ROOT.
REDACTION_REPO_ROOT="${QW_REDACTION_REPO_ROOT:-$REPO_ROOT/../../KeelMatrix.Redaction/app}"
TELEMETRY_REPO_ROOT="${QW_TELEMETRY_REPO_ROOT:-$REPO_ROOT/../../KeelMatrix.Telemetry/app}"
REDACTION_PROJECT="$REDACTION_REPO_ROOT/src/KeelMatrix.Redaction/KeelMatrix.Redaction.csproj"
TELEMETRY_PROJECT="$TELEMETRY_REPO_ROOT/src/KeelMatrix.Telemetry/KeelMatrix.Telemetry.csproj"

[[ -f "$REDACTION_PROJECT" ]] || { echo "Missing local dependency project: $REDACTION_PROJECT" >&2; exit 1; }
[[ -f "$TELEMETRY_PROJECT" ]] || { echo "Missing local dependency project: $TELEMETRY_PROJECT" >&2; exit 1; }

step ".NET SDK info"
run dotnet --info >/dev/null

ARTIFACTS="$REPO_ROOT/artifacts"
PKG_DIR="$ARTIFACTS/packages"
mkdir -p "$PKG_DIR"

step "Restore local dependency projects"
run dotnet restore "$REDACTION_PROJECT"
run dotnet restore "$TELEMETRY_PROJECT"

step "Build local shared packages (Release)"
run dotnet build "$REDACTION_PROJECT" -c Release --no-restore
run dotnet build "$TELEMETRY_PROJECT" -c Release --no-restore

step "Pack local shared packages -> ./artifacts/packages"
COMMON_PACK_ARGS=('--configuration' 'Release' '--no-build' '--include-symbols' '--p:SymbolPackageFormat=snupkg' '--output' "$PKG_DIR")
run dotnet pack "$REDACTION_PROJECT" "${COMMON_PACK_ARGS[@]}"
run dotnet pack "$TELEMETRY_PROJECT" "${COMMON_PACK_ARGS[@]}"

step "Restore and build QueryWatch solution"
run dotnet restore "KeelMatrix.QueryWatch.sln"
run dotnet build "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" -c Release --no-restore
run dotnet build "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" -c Release --no-restore

step "Pack QueryWatch libraries -> ./artifacts/packages"
run dotnet pack "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" "${COMMON_PACK_ARGS[@]}"
run dotnet pack "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" "${COMMON_PACK_ARGS[@]}"

step "Restore samples with samples/NuGet.config"
run dotnet restore "samples/QueryWatch.Samples.sln" --configfile "samples/NuGet.config"

step "Done"
echo "Packages are in: $PKG_DIR"
echo "Samples restored against local packages."
