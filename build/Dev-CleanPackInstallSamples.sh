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

step ".NET SDK info"
run dotnet --info >/dev/null

ARTIFACTS="$REPO_ROOT/artifacts"
PKG_DIR="$ARTIFACTS/packages"
mkdir -p "$PKG_DIR"

# 0) Clean local ./artifacts/packages (only KeelMatrix.QueryWatch*)
step "Clean ./artifacts/packages (KeelMatrix.QueryWatch*)"
for f in "$PKG_DIR"/KeelMatrix.QueryWatch*.nupkg "$PKG_DIR"/KeelMatrix.QueryWatch*.snupkg; do
  [[ -e "$f" ]] || continue
  echo "Deleting $f"
  rm -f "$f"
done

# 0b) Surgical cleanup: global NuGet cache (only our packages)
step "Clean global NuGet cache (KeelMatrix.QueryWatch*)"

GLOBAL_PKGS="${HOME}/.nuget/packages"
TARGETS=(
  "keelmatrix.querywatch"
  "keelmatrix.querywatch.efcore"
  "keelmatrix.querywatch.redaction"
  "keelmatrix.querywatch.contracts"
)

for name in "${TARGETS[@]}"; do
  path="$GLOBAL_PKGS/$name"
  if [[ -d "$path" ]]; then
    echo "   removing $path" >&2
    remove_with_retry "$path"
  fi
done

# 1) Restore solution
step "Restore solution"
run dotnet restore "KeelMatrix.QueryWatch.sln"

# 2) Build libraries (Release)
step "Build libraries (Release)"
run dotnet build "src/KeelMatrix.QueryWatch.Redaction/KeelMatrix.QueryWatch.Redaction.csproj" -c Release --no-restore
run dotnet build "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" -c Release --no-restore
run dotnet build "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" -c Release --no-restore

# 3) Pack libraries -> ./artifacts/packages
step "Pack libraries -> ./artifacts/packages"
COMMON_PACK_ARGS=(
  '--configuration' 'Release'
  '--no-build'
  '--include-symbols'
  '--p:SymbolPackageFormat=snupkg'
  '--output' "$PKG_DIR"
)

run dotnet pack "src/KeelMatrix.QueryWatch.Redaction/KeelMatrix.QueryWatch.Redaction.csproj" "${COMMON_PACK_ARGS[@]}"
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
