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

  $artifacts = Join-Path $repoRoot "artifacts"
  $pkgDir    = Join-Path $artifacts "packages"

  if (-not (Test-Path $pkgDir)) {
    New-Item -ItemType Directory -Path $pkgDir | Out-Null
  }

  # 0) Clean local ./artifacts/packages (only KeelMatrix.QueryWatch*)
  Step "Clean ./artifacts/packages (KeelMatrix.QueryWatch*)"
  Get-ChildItem -Path $pkgDir -Filter "KeelMatrix.QueryWatch*.nupkg" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-WithRetry $_.FullName }

  Get-ChildItem -Path $pkgDir -Filter "KeelMatrix.QueryWatch*.snupkg" -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-WithRetry $_.FullName }

  # 0b) Surgical cleanup: global NuGet cache (only our packages)
  Step "Clean global NuGet cache (KeelMatrix.QueryWatch*)"

  $globalPkgs = Join-Path $env:USERPROFILE ".nuget\packages"
  $targets = @(
    "keelmatrix.querywatch",
    "keelmatrix.querywatch.efcore",
    "keelmatrix.querywatch.redaction",
    "keelmatrix.querywatch.contracts"
  )

  foreach ($name in $targets) {
    $path = Join-Path $globalPkgs $name
    if (Test-Path $path) {
      Write-Host "   removing $path" -ForegroundColor DarkGray
      Remove-WithRetry $path
    }
  }

  # 1) Restore solution (ensures props/targets resolve correctly)
  Step "Restore solution"
  Run dotnet restore "KeelMatrix.QueryWatch.sln"

  # 2) Build in dependency-friendly order
  Step "Build libraries (Release)"
  Run dotnet build "src/KeelMatrix.QueryWatch.Redaction/KeelMatrix.QueryWatch.Redaction.csproj" -c Release --no-restore
  Run dotnet build "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" -c Release --no-restore
  Run dotnet build "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" -c Release --no-restore

  # 3) Pack all
  Step "Pack libraries -> ./artifacts/packages"
  $packArgs = @(
    '--configuration','Release',
    '--no-build',
    '--include-symbols',
    '--p:SymbolPackageFormat=snupkg',
    '--output', $pkgDir
  )

  Run dotnet pack "src/KeelMatrix.QueryWatch.Redaction/KeelMatrix.QueryWatch.Redaction.csproj" @packArgs
  Run dotnet pack "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" @packArgs
  Run dotnet pack "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" @packArgs

  # 4) Restore samples against local feed (force re-resolution)
  Step "Restore samples with samples/NuGet.config (no-cache, force)"
  Run dotnet restore "samples/QueryWatch.Samples.sln" `
    --configfile "samples/NuGet.config" `
    --no-cache `
    --force

  Step "Done"
  Write-Host "Cleaned, packed, and restored samples successfully." -ForegroundColor Green
}
catch {
  Write-Error $_
  exit 1
}
