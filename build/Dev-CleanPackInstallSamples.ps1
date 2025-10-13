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

  $artifacts = Join-Path $repoRoot "artifacts"
  $pkgDir = Join-Path $artifacts "packages"
  if (-not (Test-Path $pkgDir)) { New-Item -ItemType Directory -Path $pkgDir | Out-Null }

  # 0) Clean local KeelMatrix.* packages only (leave other artifacts intact)
  Step "Clean ./artifacts/packages (KeelMatrix.QueryWatch*)"
  Get-ChildItem -Path $pkgDir -Filter "KeelMatrix.QueryWatch*.nupkg" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
  Get-ChildItem -Path $pkgDir -Filter "KeelMatrix.QueryWatch*.snupkg" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

  # 1) Restore solution
  Step "Restore solution"
  Run dotnet restore "KeelMatrix.QueryWatch.sln"

  # 2) Build in dependency-friendly order
  Step "Build libraries (Release)"
  Run dotnet build "src/KeelMatrix.QueryWatch.Redaction/KeelMatrix.QueryWatch.Redaction.csproj" -c Release --no-restore
  Run dotnet build "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" -c Release --no-restore
  Run dotnet build "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" -c Release --no-restore

  # 3) Pack all
  Step "Pack libraries -> ./artifacts/packages"
  $packArgs = @('--configuration','Release','--no-build','--include-symbols','--p:SymbolPackageFormat=snupkg','--output',$pkgDir)
  Run dotnet pack "src/KeelMatrix.QueryWatch.Redaction/KeelMatrix.QueryWatch.Redaction.csproj" @packArgs
  Run dotnet pack "src/KeelMatrix.QueryWatch/KeelMatrix.QueryWatch.csproj" @packArgs
  Run dotnet pack "src/KeelMatrix.QueryWatch.EfCore/KeelMatrix.QueryWatch.EfCore.csproj" @packArgs

  # 4) Restore samples against local feed
  Step "Restore samples with samples/NuGet.config"
  Run dotnet restore "samples/QueryWatch.Samples.sln" --configfile "samples/NuGet.config"

  Step "Done"
  Write-Host "Cleaned, packed, and restored samples successfully." -ForegroundColor Green
}
catch {
  Write-Error $_
  exit 1
}
