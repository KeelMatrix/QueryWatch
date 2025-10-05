Param(
    # Default to the directory one level above the script’s folder (the repo root),
    # not the caller's working directory.
    [string]$RepoRoot = $(Resolve-Path (Join-Path $PSScriptRoot "..")).Path,

    # README lives at the repo root by default.
    [string]$ReadmePath = $(Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "README.md")
)

$ErrorActionPreference = 'Stop'

# Build paths deterministically
$CliProj = Join-Path $RepoRoot "tools\KeelMatrix.QueryWatch.Cli\KeelMatrix.QueryWatch.Cli.csproj"
$DocsOut = Join-Path $RepoRoot "docs\CLI_FLAGS.generated.md"

if (-not (Test-Path $CliProj)) {
    Write-Error "CLI project not found at '$CliProj'. Check RepoRoot: '$RepoRoot'.`nTip: pass -RepoRoot explicitly if your tree is non-standard."
}

Write-Host "RepoRoot: $RepoRoot" -ForegroundColor DarkGray
Write-Host "Building CLI..." -ForegroundColor Cyan
dotnet build $CliProj -c Release --nologo -v minimal
if ($LASTEXITCODE -ne 0) {
    throw "Build failed. Fix errors and run again."
}

Write-Host "Generating CLI flags snippet..." -ForegroundColor Cyan
Write-Host "Generating CLI flags snippet..." -ForegroundColor Cyan
# 1) Capture each line from dotnet run
$flagsLines = & dotnet run --project "$RepoRoot/tools/KeelMatrix.QueryWatch.Cli/KeelMatrix.QueryWatch.Cli.csproj" -- --print-flags-md
# 2) Join lines with CRLF so Markdown keeps code fence formatting
$flags = $flagsLines -join "`r`n"

$startMarker = "<!-- BEGIN:CLI_FLAGS -->"
$endMarker   = "<!-- END:CLI_FLAGS -->"

if (Test-Path $ReadmePath) {
    $readme = Get-Content -Path $ReadmePath -Raw
    if ($readme -match [regex]::Escape($startMarker) -and $readme -match [regex]::Escape($endMarker)) {
        $pattern = "(?s)" + [regex]::Escape($startMarker) + ".*?" + [regex]::Escape($endMarker)
        $replacement = "$startMarker`r`n$flags`r`n$endMarker"
        $updated = [regex]::Replace($readme, $pattern, $replacement)
        Set-Content -Path $ReadmePath -Value $updated -NoNewline
        Write-Host "README.md CLI flags section updated." -ForegroundColor Green
    } else {
        New-Item -ItemType Directory -Force -Path (Split-Path $DocsOut) | Out-Null
        Set-Content -Path $DocsOut -Value $flags -NoNewline
        Write-Warning "Markers not found in README. Wrote generated flags to: $DocsOut"
    }
} else {
    New-Item -ItemType Directory -Force -Path (Split-Path $DocsOut) | Out-Null
    Set-Content -Path $DocsOut -Value $flags -NoNewline
    Write-Warning "README not found at '$ReadmePath'. Wrote generated flags to: $DocsOut"
}
