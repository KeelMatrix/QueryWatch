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

$redactionRepoRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "..\..\KeelMatrix.Redaction\app"))
$telemetryRepoRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot "..\..\KeelMatrix.Telemetry\app"))
$redactionProject = Join-Path $redactionRepoRoot "src\KeelMatrix.Redaction\KeelMatrix.Redaction.csproj"
$telemetryProject = Join-Path $telemetryRepoRoot "src\KeelMatrix.Telemetry\KeelMatrix.Telemetry.csproj"

if (-not (Test-Path $redactionProject)) { throw "Missing local dependency project: $redactionProject" }
if (-not (Test-Path $telemetryProject)) { throw "Missing local dependency project: $telemetryProject" }

try {
  Step ".NET SDK info"
  Run dotnet --info | Out-Null

  $artifacts = Join-Path $repoRoot "artifacts"
  $pkgDir = Join-Path $artifacts "packages"
  if (-not (Test-Path $pkgDir)) { New-Item -ItemType Directory -Path $pkgDir | Out-Null }

  # 1) Restore local dependency projects first.
  Step "Restore local dependency projects"
  Run dotnet restore $redactionProject
  Run dotnet restore $telemetryProject

  # 2) Build local shared packages first.
  Step "Build local shared packages (Release)"
  Run dotnet build $redactionProject -c Release --no-restore
  Run dotnet build $telemetryProject -c Release --no-restore

  # 3) Pack shared packages so QueryWatch can restore from the local feed.
  Step "Pack local shared packages -> ./artifacts/packages"
  $packArgs = @('--configuration','Release','--no-build','--include-symbols','--p:SymbolPackageFormat=snupkg','--output',$pkgDir)
  Run dotnet pack $redactionProject @packArgs
  Run dotnet pack $telemetryProject @packArgs

  # 4) Restore and build QueryWatch after the local feed is ready.
  Step "Restore and build QueryWatch solution"
  Run dotnet restore "KeelMatrix.QueryWatch.sln"
  Run dotnet build "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" -c Release --no-restore
  Run dotnet build "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" -c Release --no-restore

  # 5) Pack QueryWatch libraries into the same local feed.
  Step "Pack QueryWatch libraries -> ./artifacts/packages"
  Run dotnet pack "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" @packArgs
  Run dotnet pack "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" @packArgs

  # 6) Restore samples using their NuGet.config (pins local KeelMatrix.* packages to ../artifacts/packages).
  Step "Restore samples with samples/NuGet.config"
  Run dotnet restore "samples/QueryWatch.Samples.sln" --configfile "samples/NuGet.config"

  Step "Done"
  Write-Host "Packages are in: $pkgDir" -ForegroundColor Green
  Write-Host "Samples restored against local packages." -ForegroundColor Green
}
catch {
  Write-Error $_
  exit 1
}
