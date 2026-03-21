#!/usr/bin/env bash
set -euo pipefail

step() { printf "\n==> %s\n" "$1"; }
run()  { printf "   %s\n" "$*" >&2; "$@"; }

# Retry helper for safe deletion (handles transient locks)
remove_with_retry() {
  local path="$1"
  local retries=3
  local delay=0.3

  for ((i=1; i<=retries; i++)); do
    if [[ ! -e "$path" ]]; then
      return 0
    fi

    if rm -rf "$path" 2>/dev/null; then
      return 0
    fi

    if [[ $i -eq $retries ]]; then
      echo "Failed to delete: $path" >&2
      return 1
    fi

    sleep "$delay"
  done
}

SCRIPT_DIR="$( cd -- "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
cd "$REPO_ROOT"

REDACTION_REPO_ROOT="$(cd "$REPO_ROOT/../../KeelMatrix.Redaction/app" && pwd)"
TELEMETRY_REPO_ROOT="$(cd "$REPO_ROOT/../../KeelMatrix.Telemetry/app" && pwd)"
REDACTION_PROJECT="$REDACTION_REPO_ROOT/src/KeelMatrix.Redaction/KeelMatrix.Redaction.csproj"
TELEMETRY_PROJECT="$TELEMETRY_REPO_ROOT/src/KeelMatrix.Telemetry/KeelMatrix.Telemetry.csproj"

step ".NET SDK info"
run dotnet --info >/dev/null

ARTIFACTS="$REPO_ROOT/artifacts"
PKG_DIR="$ARTIFACTS/packages"
mkdir -p "$PKG_DIR"

# 0) Clean local ./artifacts/packages for the QueryWatch sample dependency graph.
step "Clean ./artifacts/packages (KeelMatrix.QueryWatch*, KeelMatrix.Redaction*, KeelMatrix.Telemetry*)"
for f in "$PKG_DIR"/KeelMatrix.QueryWatch*.nupkg "$PKG_DIR"/KeelMatrix.QueryWatch*.snupkg "$PKG_DIR"/KeelMatrix.Redaction*.nupkg "$PKG_DIR"/KeelMatrix.Redaction*.snupkg "$PKG_DIR"/KeelMatrix.Telemetry*.nupkg "$PKG_DIR"/KeelMatrix.Telemetry*.snupkg; do
  [[ -e "$f" ]] || continue
  echo "Deleting $f"
  rm -f "$f"
done

# 0b) Surgical cleanup: global NuGet cache for local package resolution.
step "Clean global NuGet cache (KeelMatrix.QueryWatch*, KeelMatrix.Redaction*, KeelMatrix.Telemetry*)"

GLOBAL_PKGS="${HOME}/.nuget/packages"
TARGETS=(
  "keelmatrix.querywatch"
  "keelmatrix.querywatch.efcore"
  "keelmatrix.querywatch.contracts"
  "keelmatrix.redaction"
  "keelmatrix.telemetry"
)

for name in "${TARGETS[@]}"; do
  path="$GLOBAL_PKGS/$name"
  if [[ -d "$path" ]]; then
    echo "   removing $path" >&2
    remove_with_retry "$path"
  fi
done

# 1) Restore solution
step "Restore local dependency projects"
run dotnet restore "$REDACTION_PROJECT"
run dotnet restore "$TELEMETRY_PROJECT"

# 2) Build libraries (Release)
step "Build local shared packages (Release)"
run dotnet build "$REDACTION_PROJECT" -c Release --no-restore
run dotnet build "$TELEMETRY_PROJECT" -c Release --no-restore

# 3) Pack libraries -> ./artifacts/packages
step "Pack local shared packages -> ./artifacts/packages"
COMMON_PACK_ARGS=(
  '--configuration' 'Release'
  '--no-build'
  '--include-symbols'
  '--p:SymbolPackageFormat=snupkg'
  '--output' "$PKG_DIR"
)

run dotnet pack "$REDACTION_PROJECT" "${COMMON_PACK_ARGS[@]}"
run dotnet pack "$TELEMETRY_PROJECT" "${COMMON_PACK_ARGS[@]}"

step "Restore and build QueryWatch solution"
run dotnet restore "KeelMatrix.QueryWatch.sln"
run dotnet build "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" -c Release --no-restore
run dotnet build "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" -c Release --no-restore

step "Pack QueryWatch libraries -> ./artifacts/packages"
run dotnet pack "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" "${COMMON_PACK_ARGS[@]}"
run dotnet pack "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" "${COMMON_PACK_ARGS[@]}"

# 4) Restore samples against local feed (force re-resolution)
step "Restore samples with samples/NuGet.config (no-cache, force)"
run dotnet restore "samples/QueryWatch.Samples.sln" \
  --configfile "samples/NuGet.config" \
  --no-cache \
  --force

step "Done"
echo "Cleaned, packed, and restored samples successfully."
