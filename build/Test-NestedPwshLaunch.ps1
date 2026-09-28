[CmdletBinding()]
param(
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$helperPath = Join-Path $PSScriptRoot 'Invoke-NestedPwsh.ps1'
$guardPath = $PSCommandPath

function Get-LaunchViolations([string]$Path) {
    $tokens = $null
    $parseErrors = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count -gt 0) {
        return @("${Path} contains PowerShell parse errors.")
    }

    $source = [IO.File]::ReadAllText($Path)
    $violations = [System.Collections.Generic.List[string]]::new()
    $commands = $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)
    foreach ($command in $commands) {
        $nameAst = $command.CommandElements[0]
        $commandName = if ($nameAst -is [System.Management.Automation.Language.StringConstantExpressionAst]) {
            $nameAst.Value
        }
        elseif ($nameAst -is [System.Management.Automation.Language.ExpandableStringExpressionAst]) {
            $nameAst.Value
        }
        else {
            $null
        }

        if ($commandName -match '^(?i:pwsh|powershell)(?:\.exe)?$') {
            [void]$violations.Add("${Path}:$($command.Extent.StartLineNumber): direct nested PowerShell launch")
            continue
        }

        $literalArguments = @($command.CommandElements | Select-Object -Skip 1 | Where-Object {
                $_ -is [System.Management.Automation.Language.StringConstantExpressionAst]
            } | ForEach-Object { $_.Value })
        if ($literalArguments | Where-Object { $_ -match '^(?i:pwsh|powershell)(?:\.exe)?$' }) {
            [void]$violations.Add("${Path}:$($command.Extent.StartLineNumber): nested PowerShell executable passed to '$commandName'")
        }

        if ($commandName -eq 'Start-Process' -and
            $source -notmatch '(?i)(?:-WindowStyle\s+[''"]?Hidden|(?:\.)?WindowStyle\s*=\s*[''"]?Hidden|CreateNoWindow|NoNewWindow)') {
            [void]$violations.Add("${Path}:$($command.Extent.StartLineNumber): Start-Process lacks hidden-window containment")
        }
    }

    return $violations.ToArray()
}

if ($SelfTest) {
    $selfTestRoot = Join-Path ([IO.Path]::GetTempPath()) "nested-pwsh-guard-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $selfTestRoot -Force | Out-Null
    try {
        $directPath = Join-Path $selfTestRoot 'direct.ps1'
        $processPath = Join-Path $selfTestRoot 'process.ps1'
        $safePath = Join-Path $selfTestRoot 'safe.ps1'
        [IO.File]::WriteAllText($directPath, '& pwsh -NoProfile')
        [IO.File]::WriteAllText($processPath, "Start-Process 'example.exe'")
        [IO.File]::WriteAllText($safePath, "Invoke-NestedPwsh -ArgumentList @('-NoProfile')")
        if (@(Get-LaunchViolations $directPath).Count -eq 0) {
            throw 'The guard self-test did not reject a direct nested PowerShell launch.'
        }
        if (@(Get-LaunchViolations $processPath).Count -eq 0) {
            throw 'The guard self-test did not reject a visible Start-Process launch.'
        }
        if (@(Get-LaunchViolations $safePath).Count -ne 0) {
            throw 'The guard self-test rejected a helper-mediated launch.'
        }
    }
    finally {
        Remove-Item -LiteralPath $selfTestRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    Write-Output 'Nested PowerShell launch guard self-test passed.'
    exit 0
}

if (-not (Test-Path -LiteralPath $helperPath -PathType Leaf)) {
    throw "Shared nested PowerShell launch helper is missing: $helperPath"
}

$scriptFiles = Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter '*.ps1' |
    Where-Object {
        $_.FullName -notin @($helperPath, $guardPath) -and
        $_.FullName -notmatch '[\\/]((\.git)|(bin)|(obj)|(artifacts)|_probe[\\/]corpus)([\\/]|$)'
    }
$violations = @($scriptFiles | ForEach-Object { Get-LaunchViolations $_.FullName })
if ($violations.Count -gt 0) {
    throw "Visible child process launch sites must use the shared containment helper."
}

Write-Output 'Nested PowerShell launch guard passed.'
