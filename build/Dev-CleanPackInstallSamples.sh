#!/usr/bin/env bash
set -euo pipefail

step() { printf "\n==> %s\n" "$1"; }
run()  { printf "   %s\n" "$*" >&2; "$@"; }

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

step "Clean local QueryWatch packages"
for f in "$PKG_DIR"/KeelMatrix.QueryWatch*.nupkg "$PKG_DIR"/KeelMatrix.QueryWatch*.snupkg "$PKG_DIR"/qwatch*.nupkg "$PKG_DIR"/qwatch*.snupkg; do
  [[ -e "$f" ]] || continue
  echo "Deleting $f"
  rm -f "$f"
done

step "Clean global QueryWatch package caches"
GLOBAL_PKGS="${HOME}/.nuget/packages"
for name in keelmatrix.querywatch keelmatrix.querywatch.efcore qwatch; do
  path="$GLOBAL_PKGS/$name"
  if [[ -d "$path" ]]; then
    echo "   removing $path" >&2
    remove_with_retry "$path"
  fi
done

step "Restore QueryWatch from NuGet.org"
run dotnet restore "KeelMatrix.QueryWatch.sln" --configfile "NuGet.config" --no-cache --force

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
run dotnet restore "samples/QueryWatch.Samples.sln" --configfile "samples/NuGet.config" --no-cache --force

step "Done"
echo "Cleaned, packed, and restored samples successfully."
