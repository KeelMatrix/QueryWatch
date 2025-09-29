<# 
.SYNOPSIS
  Discover and run all BenchmarkDotNet projects under the ./bench folder, collect artifacts,
  and (optionally) fail on noisy results.

.DESCRIPTION
  - Finds all *.Benchmarks.csproj under the folder containing this script (./bench).
  - Runs `dotnet run -c Release -f <Framework>` in each project with BenchmarkDotNet args.
  - Exports CSV/JSON/Markdown into artifacts/benchmarks/<timestamp>/<ProjectName>/ at repo root.
  - Optionally checks relative StdDev% against a threshold and fails.
  - Can emit a compact CI summary (Markdown).

.PARAMETER Filter
  BenchmarkDotNet filter (e.g., '*Phone*', '*Redactor*'). Default '*'.

.PARAMETER Framework
  Target TF to run (default 'net8.0'). Change if your benches target another TFM.

.PARAMETER Job
  Suggested BDN job preset to hint your Program.cs switcher (Default|Short|Medium|Long). Passed through as '--job'.

.PARAMETER ArtifactsRoot
  Root artifacts folder **relative to repo root** (default 'artifacts/benchmarks').

.PARAMETER MaxStdevPct
  If > 0, fail the script when any benchmark StdDev/Mean exceeds this percent (e.g., 10 for 10%).

.PARAMETER Ci
  If set, prints a compact Markdown summary suitable for CI step summaries.

.PARAMETER DryRun
  If set, only lists the benchmark projects it would run.

.EXAMPLES
  # Run all benches, short job, net8.0, export artifacts
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1

  # Filter by name and fail if noise > 7.5%
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1 -Filter '*Phone*' -MaxStdevPct 7.5

  # Medium job on net9.0
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1 -Framework net9.0 -Job Medium

  # CI-friendly output
  pwsh -NoProfile -File bench/Run-Benchmarks.ps1 -Ci -MaxStdevPct 12
#>

[CmdletBinding()]
param(
  [string]$Filter = '*',
  [ValidateSet('Default','Short','Medium','Long')]
  [string]$Job = 'Default',
  [string]$Framework = 'net8.0',
  [string]$ArtifactsRoot = 'artifacts/benchmarks',
  [double]$MaxStdevPct = 0,
  [switch]$Ci,
  [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Heading($text){ Write-Host "=== $text ===" -ForegroundColor Cyan }
function New-Timestamp(){ Get-Date -Format 'yyyyMMdd-HHmmss' }

# The script sits in ./bench; repo root is its parent folder.
$ScriptDir = Split-Path -Parent $PSCommandPath
$RepoRoot  = Resolve-Path (Join-Path $ScriptDir '..')

Set-Location $RepoRoot

# Discover benchmark projects **only under the bench folder**
$BenchRoot = $ScriptDir
Write-Heading "Discovering benchmark projects under $BenchRoot"
# Wrap in @() to force an array even if a single item is returned
$benchProjects = @(
  Get-ChildItem -Path $BenchRoot -Recurse -Filter *.Benchmarks.csproj -ErrorAction SilentlyContinue |
  Select-Object -ExpandProperty FullName
)

if(-not $benchProjects -or $benchProjects.Count -eq 0){
  Write-Warning "No *.Benchmarks.csproj found under $BenchRoot."
  exit 2
}

$stamp = New-Timestamp
# Artifacts path is relative to repo root.
$runRoot = Join-Path $RepoRoot (Join-Path $ArtifactsRoot $stamp)
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

Write-Host "Artifacts path: $runRoot"

if($DryRun){
  Write-Heading "Dry-Run: would run these projects"
  $benchProjects | ForEach-Object { Write-Host " - $_" }
  exit 0
}

# Ensure dotnet is available
& dotnet --info | Out-Null

$total = 0
$failed = 0
$noiseBreaches = @()
$allCsv = @()

foreach($proj in $benchProjects){
  $projName = Split-Path $proj -LeafBase
  $projOut = Join-Path $runRoot $projName
  New-Item -ItemType Directory -Force -Path $projOut | Out-Null

  Write-Heading "Running $projName ($Framework, Job: $Job, Filter: $Filter)"
  Push-Location (Split-Path $proj -Parent)

  # BenchmarkDotNet common CLI args (supported by BenchmarkSwitcher):
  #   --list, --filter, --runtimes, --artifacts, --exporters, --join, --job
  # Multiple exporters: pass them as separate values to a **single** --exporters option.
  $bdnArgs = @(
    '--filter', "$Filter",
    '--exporters', 'json', 'markdown',
    '--artifacts', "$projOut",
    '--join'
  )
  if($Job -ne 'Default'){ $bdnArgs += @('--job', $Job) }

  # dotnet run to bench project
  $cmd = @(
    'run','--project',"$proj",
    '-c','Release',
    '-f',"$Framework",
    '--'
  ) + $bdnArgs

  Write-Host ("dotnet " + ($cmd -join ' '))
  $proc = Start-Process -FilePath 'dotnet' -ArgumentList $cmd -PassThru -Wait -NoNewWindow
  if($proc.ExitCode -ne 0){
    Write-Error "Benchmark run failed for $projName with exit code $($proc.ExitCode)."
    $failed++
    Pop-Location
    continue
  }
  Pop-Location

  # Collect CSVs to evaluate noise and to print summary
  $csvs = Get-ChildItem -Path $projOut -Recurse -Filter *.csv -ErrorAction SilentlyContinue
  if($csvs){
    $allCsv += $csvs.FullName
  }
  $total++
}

Write-Heading "Post-processing results"

# Parse CSVs for noise/summary. BenchmarkDotNet CSV has columns like:
# Method,Mean,Error,StdDev,Median,Allocated
function TryParse-Number([string]$s){
  if([string]::IsNullOrWhiteSpace($s)){ return $null }
  # Handle both '.' and ',' decimal separators by removing thousand sep and normalizing decimal.
  $ns = $s.Trim() -replace '[^\d\.,-]' # drop units
  if($ns -match '^\d{1,3}(\.\d{3})+,\d+$'){ $ns = $ns -replace '\.','' -replace ',','.' }
  elseif($ns -match '^\d{1,3}(,\d{3})+\.\d+$'){ $ns = $ns -replace ',','' }
  [double]::TryParse($ns, [System.Globalization.CultureInfo]::InvariantCulture, [ref]([double]$out = 0)) | Out-Null
  if($?) { return $out } else { return $null }
}

$rows = @()
foreach($csv in $allCsv){
  try{
    $content = Get-Content $csv
    if(-not $content){ continue }
    $header = $content[0].Split(',')
    # Map indexes
    $iMethod   = [Array]::IndexOf($header,'Method')
    $iMean     = [Array]::IndexOf($header,'Mean')
    $iStdDev   = [Array]::IndexOf($header,'StdDev')
    $iAllocated= [Array]::IndexOf($header,'Allocated')
    for($i=1; $i -lt $content.Count; $i++){
      $cols = $content[$i].Split(',')
      if($cols.Count -lt 2){ continue }
      $method = if($iMethod -ge 0){ $cols[$iMethod] } else { '(unknown)' }
      $mean   = if($iMean   -ge 0){ TryParse-Number $cols[$iMean] } else { $null }
      $std    = if($iStdDev -ge 0){ TryParse-Number $cols[$iStdDev] } else { $null }
      $alloc  = if($iAllocated -ge 0){ $cols[$iAllocated] } else { $null }

      $pct = $null
      if($mean -and $mean -ne 0 -and $std -ne $null){
        $pct = [math]::Round(($std / $mean) * 100.0, 2)
      }
      $rows += [pscustomobject]@{
        Csv = $csv
        Method = $method
        Mean   = $mean
        StdDev = $std
        StdDevPct = $pct
        Allocated = $alloc
      }

      if($MaxStdevPct -gt 0 -and $pct -ne $null -and $pct -gt $MaxStdevPct){
        $noiseBreaches += [pscustomobject]@{
          Csv = $csv; Method = $method; StdDevPct = $pct; Limit = $MaxStdevPct
        }
      }
    }
  } catch {
    Write-Warning "Failed to parse CSV: $csv. $_"
  }
}

# Emit CI summary (compact)
if($Ci){
  $md = New-Object System.Text.StringBuilder
  [void]$md.AppendLine("# Benchmark Summary ($stamp)")
  [void]$md.AppendLine()
  [void]$md.AppendLine("**Projects:** $total  |  **CSV Files:** $($allCsv.Count)")
  if($MaxStdevPct -gt 0){
    if($noiseBreaches.Count -gt 0){
      [void]$md.AppendLine()
      [void]$md.AppendLine("**Noise breaches (> $MaxStdevPct% StdDev):**")
      foreach($b in $noiseBreaches){
        [void]$md.AppendLine((" - `{0}` :: `{1}` → {2}%") -f (Split-Path $b.Csv -Leaf), $b.Method, $b.StdDevPct)
      }
    } else {
      [void]$md.AppendLine()
      [void]$md.AppendLine("No noise breaches (StdDev <= $MaxStdevPct%).")
    }
  }
  $summaryPath = Join-Path $runRoot "SUMMARY.md"
  $md.ToString() | Set-Content -Encoding UTF8 $summaryPath
  Write-Host ""
  Write-Host "CI Summary written to $summaryPath"
}

# Exit code logic
if($failed -gt 0){
  Write-Error "$failed project(s) failed to run."
  exit 3
}
if($noiseBreaches.Count -gt 0){
  Write-Error "$($noiseBreaches.Count) benchmark(s) exceeded StdDev% limit ($MaxStdevPct%)."
  exit 4
}

Write-Heading "Done"
Write-Host "Artifacts: $runRoot"
exit 0
