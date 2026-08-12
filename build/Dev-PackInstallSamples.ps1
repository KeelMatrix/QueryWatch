Param()
$ErrorActionPreference = 'Stop'

function Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Run {
  param([Parameter(Mandatory=$true)][string]$exe, [Parameter(ValueFromRemainingArguments=$true)][string[]]$args)
  Write-Host ("   " + $exe + " " + ($args -join " ")) -ForegroundColor DarkGray
  & $exe @args
  if ($LASTEXITCODE -ne 0) { throw "Command failed: $exe $($args -join ' ')" }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

try {
  Step ".NET SDK info"
  Run dotnet --info | Out-Null

  $pkgDir = Join-Path (Join-Path $repoRoot "artifacts") "packages"
  if (-not (Test-Path $pkgDir)) { New-Item -ItemType Directory -Path $pkgDir | Out-Null }

  Step "Restore QueryWatch from NuGet.org"
  Run dotnet restore "KeelMatrix.QueryWatch.sln" --configfile "NuGet.config"

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
  Run dotnet restore "samples/QueryWatch.Samples.sln" --configfile "samples/NuGet.config"

  Step "Done"
  Write-Host "Packages are in: $pkgDir" -ForegroundColor Green
  Write-Host "Samples restored with QueryWatch packages from the local feed and Redaction/Telemetry from NuGet.org." -ForegroundColor Green
}
catch {
  Write-Error $_
  exit 1
}
