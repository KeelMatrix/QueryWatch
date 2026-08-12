Param()
$ErrorActionPreference = 'Stop'

function Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

function Run {
  param(
    [Parameter(Mandatory=$true)][string]$exe,
    [Parameter(ValueFromRemainingArguments=$true)][string[]]$args
  )
  Write-Host ("   " + $exe + " " + ($args -join " ")) -ForegroundColor DarkGray
  & $exe @args
  if ($LASTEXITCODE -ne 0) { throw "Command failed: $exe $($args -join ' ')" }
}

function Remove-WithRetry {
  param(
    [Parameter(Mandatory=$true)][string]$Path,
    [int]$Retries = 3,
    [int]$DelayMs = 300
  )

  for ($i = 1; $i -le $Retries; $i++) {
    try {
      if (Test-Path $Path) {
        Remove-Item $Path -Recurse -Force -ErrorAction Stop
      }
      return
    }
    catch {
      if ($i -eq $Retries) { throw }
      Start-Sleep -Milliseconds $DelayMs
    }
  }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

try {
  Step ".NET SDK info"
  Run dotnet --info | Out-Null

  $pkgDir = Join-Path (Join-Path $repoRoot "artifacts") "packages"
  if (-not (Test-Path $pkgDir)) { New-Item -ItemType Directory -Path $pkgDir | Out-Null }

  Step "Clean local QueryWatch packages"
  @("KeelMatrix.QueryWatch*.nupkg", "KeelMatrix.QueryWatch*.snupkg", "qwatch*.nupkg", "qwatch*.snupkg") |
    ForEach-Object {
      Get-ChildItem -Path $pkgDir -Filter $_ -ErrorAction SilentlyContinue |
        ForEach-Object { Remove-WithRetry $_.FullName }
    }

  Step "Clean global QueryWatch package caches"
  $globalPkgs = Join-Path $env:USERPROFILE ".nuget\packages"
  @("keelmatrix.querywatch", "keelmatrix.querywatch.efcore", "qwatch") |
    ForEach-Object {
      $path = Join-Path $globalPkgs $_
      if (Test-Path $path) {
        Write-Host "   removing $path" -ForegroundColor DarkGray
        Remove-WithRetry $path
      }
    }

  Step "Restore QueryWatch from NuGet.org"
  Run dotnet restore "KeelMatrix.QueryWatch.sln" --configfile "NuGet.config" --no-cache --force

  Step "Build QueryWatch libraries (Release)"
  Run dotnet build "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" -c Release --no-restore
  Run dotnet build "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" -c Release --no-restore

  Step "Pack QueryWatch libraries -> ./artifacts/packages"
  $packArgs = @(
    '--configuration','Release',
    '--no-build',
    '--include-symbols',
    '--p:SymbolPackageFormat=snupkg',
    '--p:Version=0.1.0',
    '--output', $pkgDir
  )
  Run dotnet pack "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" @packArgs
  Run dotnet pack "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" @packArgs

  Step "Restore samples with their local QueryWatch package feed"
  Run dotnet restore "samples/QueryWatch.Samples.sln" --configfile "samples/NuGet.config" --no-cache --force

  Step "Done"
  Write-Host "Cleaned, packed, and restored samples successfully." -ForegroundColor Green
}
catch {
  Write-Error $_
  exit 1
}
