[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Repo,

    [Parameter(Mandatory = $false)]
    [ValidateSet("open", "dismissed", "fixed", "all")]
    [string]$State = "open",

    [Parameter(Mandatory = $false)]
    [string]$OutputRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:GitHubCliCommand = $null

function Fail {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [int]$Code = 1
    )

    Write-Error $Message
    exit $Code
}

function Require-Command {
    param([Parameter(Mandatory = $true)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Fail "Required command '$Name' was not found in PATH. Install it first and re-run the script."
    }
}

function Resolve-GhCommand {
    $command = Get-Command gh, gh.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command) { return $command.Source }

    $candidates = @(
        'C:\Program Files\GitHub CLI\gh.exe',
        'C:\Program Files (x86)\GitHub CLI\gh.exe',
        (Join-Path $env:LOCALAPPDATA 'GitHub CLI\gh.exe')
    ) | Where-Object { $_ -and (Test-Path $_) }

    return $candidates | Select-Object -First 1
}

function Get-ScriptRootSafe {
    if ($PSScriptRoot) { return $PSScriptRoot }
    return (Get-Location).Path
}

function Get-RepoFromGitRemote {
    try {
        $remoteUrl = git remote get-url origin 2>$null
        if (-not $remoteUrl) { return $null }

        if ($remoteUrl -match 'github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)(?:\.git)?$') {
            return "$($Matches.owner)/$($Matches.repo)"
        }

        return $null
    }
    catch {
        return $null
    }
}

function Get-RepoName {
    param([string]$ExplicitRepo)

    if ($ExplicitRepo) { return $ExplicitRepo }

    try {
        $nameWithOwner = & $script:GitHubCliCommand repo view --json nameWithOwner --jq .nameWithOwner 2>$null
        if ($nameWithOwner) { return $nameWithOwner.Trim() }
    }
    catch {
    }

    $fromGit = Get-RepoFromGitRemote
    if ($fromGit) { return $fromGit }

    Fail "Could not determine the GitHub repository automatically. Run the script from a cloned GitHub repo or pass -Repo 'owner/name'."
}

function Assert-GhAuth {
    try {
        $null = & $script:GitHubCliCommand auth status 2>$null
    }
    catch {
        Fail "GitHub CLI is installed but not authenticated. Run 'gh auth login' first."
    }
}

function Get-JsonProp {
    param(
        [Parameter(Mandatory = $false)]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        $Default = $null
    )

    if ($null -eq $Object) { return $Default }

    $prop = $Object.PSObject.Properties[$Name]
    if ($null -eq $prop) { return $Default }
    return $prop.Value
}

function Invoke-GhApiPagedArray {
    param([Parameter(Mandatory = $true)][string]$ApiPath)

    try {
        $rawOutput = & $script:GitHubCliCommand api $ApiPath --paginate --header "Accept: application/vnd.github+json" --header "X-GitHub-Api-Version: 2022-11-28"
        $rawText = ($rawOutput | Out-String).Trim()
        if (-not $rawText) {
            return @()
        }

        $items = New-Object System.Collections.Generic.List[object]

        $chunks = $rawText -split "`r?`n(?=\s*\[)"
        foreach ($chunk in $chunks) {
            $text = $chunk.Trim()
            if (-not $text) { continue }

            $parsed = $text | ConvertFrom-Json -ErrorAction Stop
            if ($parsed -is [System.Array]) {
                foreach ($p in $parsed) {
                    [void]$items.Add($p)
                }
            }
            else {
                [void]$items.Add($parsed)
            }
        }

        return @($items.ToArray())
    }
    catch {
        $message = $_.Exception.Message
        Fail "GitHub API request failed for path '$ApiPath'. Details: $message"
    }
}

function Get-LocationKind {
    param([string]$Path)

    if (-not $Path) { return "unknown" }

    $p = $Path.Replace('\', '/').ToLowerInvariant()

    if ($p.StartsWith('.github/workflows/')) { return 'workflow' }
    if ($p -match '(^|/)_deps/') { return 'dependency' }
    if ($p -match '(^|/)(tests?|test)/') { return 'tests' }
    if ($p -match '(^|/)(obj|bin|generated|gen)/') { return 'generated' }
    if ($p -match '(^|/)(src|source|lib|tools)/') { return 'src' }

    return 'unknown'
}

function Get-CategoryPrimary {
    param(
        [string]$LocationKind,
        [string[]]$Tags,
        [string]$RuleId,
        [string]$Path
    )

    $tagsText = (($Tags | Where-Object { $_ }) -join ',').ToLowerInvariant()
    $rule = ($RuleId ?? '').ToLowerInvariant()
    $p = (($Path ?? '').Replace('\', '/')).ToLowerInvariant()

    if ($LocationKind -eq 'workflow') { return 'workflow' }
    if ($LocationKind -eq 'dependency') { return 'dependency' }
    if ($LocationKind -eq 'generated') { return 'generated-code' }
    if ($LocationKind -eq 'tests') { return 'test-only' }

    if ($tagsText -match 'security') { return 'security' }
    if ($tagsText -match 'correctness') { return 'correctness' }
    if ($tagsText -match 'reliability') { return 'reliability' }
    if ($tagsText -match 'performance') { return 'performance' }
    if ($tagsText -match 'maintainability') { return 'maintainability' }

    if ($rule -match 'actions|workflow|permissions|unpinned') { return 'workflow' }
    if ($p -match '\.github/workflows/') { return 'workflow' }

    return 'other'
}

function Get-CategorySecondary {
    param(
        [string]$LocationKind,
        [string[]]$Tags,
        [string]$RuleId
    )

    $tagsText = (($Tags | Where-Object { $_ }) -join ',').ToLowerInvariant()
    $rule = ($RuleId ?? '').ToLowerInvariant()

    if ($LocationKind -eq 'workflow') {
        if ($rule -match 'permissions') { return 'actions-hardening' }
        if ($rule -match 'unpinned') { return 'dependency-workflow' }
        return 'workflow-policy'
    }

    if ($LocationKind -eq 'tests') { return 'test-harness' }
    if ($LocationKind -eq 'generated') { return 'generated-code' }
    if ($LocationKind -eq 'dependency') { return 'vendored-dependency' }

    if ($rule -match 'null|deref') { return 'null-safety' }
    if ($rule -match 'dispose|disposable|resource') { return 'resource-disposal' }
    if ($rule -match 'exception|catch|throw') { return 'exception-handling' }
    if ($rule -match 'dead|unused|constant') { return 'dead-code' }
    if ($rule -match 'regex') { return 'generated-regex' }
    if ($rule -match 'input|validate') { return 'input-validation' }
    if ($tagsText -match 'performance') { return 'performance' }
    if ($tagsText -match 'maintainability') { return 'maintainability' }
    if ($tagsText -match 'correctness') { return 'correctness' }

    return 'other'
}

function Get-TriageBucket {
    param(
        [string]$LocationKind,
        [string]$CategoryPrimary,
        [string]$Severity,
        [string]$SecuritySeverity
    )

    $sev = ($Severity ?? '').ToLowerInvariant()
    $sec = ($SecuritySeverity ?? '').ToLowerInvariant()

    if ($LocationKind -eq 'workflow') { return 'fix_now' }
    if ($LocationKind -eq 'src' -and ($sec -in @('critical', 'high', 'medium') -or $sev -in @('error', 'warning'))) { return 'fix_now' }
    if ($LocationKind -eq 'dependency') { return 'likely_dependency_noise' }
    if ($CategoryPrimary -in @('generated-code', 'test-only')) { return 'likely_dismiss_generated_or_test' }
    if ($LocationKind -in @('generated', 'tests')) { return 'likely_dismiss_generated_or_test' }

    return 'review_manually'
}

function Group-Count {
    param(
        [Parameter(Mandatory = $true)][object[]]$Items,
        [Parameter(Mandatory = $true)][string]$Property
    )

    return @(
        $Items |
        Group-Object -Property $Property |
        Sort-Object Count -Descending |
        ForEach-Object {
            [PSCustomObject]@{
                name  = if ([string]::IsNullOrWhiteSpace($_.Name)) { '<null>' } else { $_.Name }
                count = $_.Count
            }
        }
    )
}

try {
    Require-Command -Name 'git'
    $script:GitHubCliCommand = Resolve-GhCommand
    if (-not $script:GitHubCliCommand) {
        Fail "Required command 'gh' was not found in PATH or standard install locations. Install GitHub CLI first and re-run the script."
    }
    Assert-GhAuth

    $resolvedRepo = Get-RepoName -ExplicitRepo $Repo

    $scriptRoot = Get-ScriptRootSafe
    $repoRoot = Resolve-Path (Join-Path $scriptRoot '..') | Select-Object -ExpandProperty Path

    if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
        $OutputRoot = Join-Path $repoRoot 'build\artifacts\security'
    }

    if (-not (Test-Path $OutputRoot)) {
        $null = New-Item -ItemType Directory -Path $OutputRoot -Force
    }

    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $safeRepoName = $resolvedRepo.Replace('/', '__')

    $stateParam = if ($State -eq 'all') { '?per_page=100' } else { "?state=$State&per_page=100" }
    $apiPath = "/repos/$resolvedRepo/code-scanning/alerts$stateParam"

    Write-Host "Repository: $resolvedRepo"
    Write-Host 'Fetching Code Scanning alerts from GitHub...'
    $alerts = Invoke-GhApiPagedArray -ApiPath $apiPath
    if (-not $alerts) { $alerts = @() }

    $categorized = @(
        foreach ($a in $alerts) {
            $rule = Get-JsonProp -Object $a -Name 'rule'
            $tool = Get-JsonProp -Object $a -Name 'tool'
            $instance = Get-JsonProp -Object $a -Name 'most_recent_instance'
            $location = Get-JsonProp -Object $instance -Name 'location'
            $messageObj = Get-JsonProp -Object $instance -Name 'message'

            $path = Get-JsonProp -Object $location -Name 'path' -Default ''
            $line = Get-JsonProp -Object $location -Name 'start_line'
            $tags = @((Get-JsonProp -Object $rule -Name 'tags' -Default @()))
            $classifications = @((Get-JsonProp -Object $instance -Name 'classifications' -Default @()))
            $locationKind = Get-LocationKind -Path $path
            $ruleId = Get-JsonProp -Object $rule -Name 'id' -Default ''
            $severity = Get-JsonProp -Object $rule -Name 'severity' -Default ''
            $securitySeverity = Get-JsonProp -Object $rule -Name 'security_severity_level' -Default $null
            $primary = Get-CategoryPrimary -LocationKind $locationKind -Tags $tags -RuleId $ruleId -Path $path
            $secondary = Get-CategorySecondary -LocationKind $locationKind -Tags $tags -RuleId $ruleId
            $triageBucket = Get-TriageBucket -LocationKind $locationKind -CategoryPrimary $primary -Severity $severity -SecuritySeverity $securitySeverity

            [PSCustomObject]@{
                number             = Get-JsonProp -Object $a -Name 'number'
                state              = Get-JsonProp -Object $a -Name 'state'
                created_at         = Get-JsonProp -Object $a -Name 'created_at'
                updated_at         = Get-JsonProp -Object $a -Name 'updated_at'
                fixed_at           = Get-JsonProp -Object $a -Name 'fixed_at'
                dismissed_at       = Get-JsonProp -Object $a -Name 'dismissed_at'
                rule               = $ruleId
                severity           = $severity
                security_severity  = $securitySeverity
                tool               = Get-JsonProp -Object $tool -Name 'name' -Default ''
                file               = $path
                line               = $line
                message            = Get-JsonProp -Object $messageObj -Name 'text' -Default ''
                tags               = $tags
                classifications    = $classifications
                category_primary   = $primary
                category_secondary = $secondary
                location_kind      = $locationKind
                triage_bucket      = $triageBucket
                html_url           = Get-JsonProp -Object $a -Name 'html_url'
                commit_sha         = Get-JsonProp -Object $instance -Name 'commit_sha'
                analysis_key       = Get-JsonProp -Object $instance -Name 'analysis_key'
                environment        = Get-JsonProp -Object $instance -Name 'environment'
            }
        }
    )

    $summary = [PSCustomObject]@{
        repository                   = $resolvedRepo
        generated_at_utc             = (Get-Date).ToUniversalTime().ToString('o')
        total_alerts                 = $categorized.Count
        counts_by_state              = Group-Count -Items $categorized -Property 'state'
        counts_by_severity           = Group-Count -Items $categorized -Property 'severity'
        counts_by_security_severity  = Group-Count -Items $categorized -Property 'security_severity'
        counts_by_rule               = Group-Count -Items $categorized -Property 'rule'
        counts_by_category_primary   = Group-Count -Items $categorized -Property 'category_primary'
        counts_by_category_secondary = Group-Count -Items $categorized -Property 'category_secondary'
        counts_by_location_kind      = Group-Count -Items $categorized -Property 'location_kind'
        counts_by_triage_bucket      = Group-Count -Items $categorized -Property 'triage_bucket'
        top_20_files = @(
            $categorized |
            Group-Object file |
            Sort-Object Count -Descending |
            Select-Object -First 20 |
            ForEach-Object {
                [PSCustomObject]@{
                    file  = if ([string]::IsNullOrWhiteSpace($_.Name)) { '<null>' } else { $_.Name }
                    count = $_.Count
                }
            }
        )
        top_20_rules = @(
            $categorized |
            Group-Object rule |
            Sort-Object Count -Descending |
            Select-Object -First 20 |
            ForEach-Object {
                [PSCustomObject]@{
                    rule  = if ([string]::IsNullOrWhiteSpace($_.Name)) { '<null>' } else { $_.Name }
                    count = $_.Count
                }
            }
        )
        recommended_triage_buckets = [PSCustomObject]@{
            fix_now                           = @($categorized | Where-Object { $_.triage_bucket -eq 'fix_now' } | Select-Object -ExpandProperty number)
            review_manually                  = @($categorized | Where-Object { $_.triage_bucket -eq 'review_manually' } | Select-Object -ExpandProperty number)
            likely_dismiss_generated_or_test = @($categorized | Where-Object { $_.triage_bucket -eq 'likely_dismiss_generated_or_test' } | Select-Object -ExpandProperty number)
            likely_dependency_noise          = @($categorized | Where-Object { $_.triage_bucket -eq 'likely_dependency_noise' } | Select-Object -ExpandProperty number)
        }
    }

    $rawPath = Join-Path $OutputRoot "$safeRepoName.code-scanning-alerts.$timestamp.raw.json"
    $categorizedPath = Join-Path $OutputRoot "$safeRepoName.code-scanning-alerts.$timestamp.categorized.json"
    $summaryPath = Join-Path $OutputRoot "$safeRepoName.code-scanning-alerts.$timestamp.summary.json"

    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($rawPath, ($alerts | ConvertTo-Json -Depth 100), $utf8NoBom)
    [System.IO.File]::WriteAllText($categorizedPath, ($categorized | ConvertTo-Json -Depth 100), $utf8NoBom)
    [System.IO.File]::WriteAllText($summaryPath, ($summary | ConvertTo-Json -Depth 100), $utf8NoBom)

    Write-Host ''
    Write-Host 'Done.'
    Write-Host "Raw JSON:        $rawPath"
    Write-Host "Categorized:     $categorizedPath"
    Write-Host "Summary:         $summaryPath"
    Write-Host ''
    Write-Host "Total alerts:    $($summary.total_alerts)"
    Write-Host "Fix now:         $($summary.recommended_triage_buckets.fix_now.Count)"
    Write-Host "Review manually: $($summary.recommended_triage_buckets.review_manually.Count)"
    Write-Host "Generated/tests: $($summary.recommended_triage_buckets.likely_dismiss_generated_or_test.Count)"
    Write-Host "Dependency noise:$($summary.recommended_triage_buckets.likely_dependency_noise.Count)"
}
catch {
    $msg = $_.Exception.Message
    Fail "Unexpected failure while fetching or categorizing alerts. Details: $msg"
}
